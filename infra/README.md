# TaskTracker — dev environment (Bicep)

Provisions the whole dev environment: ACR, Log Analytics + App Insights, Container Apps
environment, three container apps, the DbUp migration job, Postgres, Storage, Service Bus,
Cosmos, ACS and Key Vault.

Azure OpenAI and AI Search are **not** created — you already own them, so their endpoints and
keys come in as parameters.

## 1. Manual prerequisite: one Entra app registration

The app uses a single registration as both the API and the SPA client (compose passes the same
`AZURE_CLIENT_ID` to both), so create one:

```bash
az ad app create --display-name "TaskTracker dev" --sign-in-audience AzureADandPersonalMicrosoftAccount
```

Then, in Entra portal → App registrations → TaskTracker dev:

1. **Expose an API** → set Application ID URI to `api://<client-id>` → Add scope
   `access_as_user`, admin + user consent.
2. **Authentication** → Add platform → **Single-page application**. Leave the redirect URI blank
   for now; you get the real one from the deployment output in step 3.
3. **API permissions** → Add a permission → My APIs → TaskTracker dev → `access_as_user` → Grant
   admin consent.

Note the **Application (client) ID** and **Directory (tenant) ID**.

## 2. Set the deployment variables

```bash
export AZURE_TENANT_ID="<tenant-id>"
export AZURE_CLIENT_ID="<client-id>"

export AZURE_OPENAI_ENDPOINT="https://<your-aoai>.openai.azure.com"
export AZURE_OPENAI_DEPLOYMENT="<chat deployment name>"
export AZURE_OPENAI_API_KEY="<key>"
export AZURE_AI_SEARCH_ENDPOINT="https://<your-search>.search.windows.net"
export AZURE_AI_SEARCH_INDEX="<index name>"
export AZURE_AI_SEARCH_API_KEY="<key>"

export POSTGRES_ADMIN_PASSWORD="$(openssl rand -base64 24)"
export INTERNAL_API_KEY="$(uuidgen)"
export STRIPE_SECRET_KEY="sk_test_..."
export STRIPE_WEBHOOK_SECRET="whsec_placeholder"   # real value in step 6
export CLIENT_IP="$(curl -s ifconfig.me)"          # optional, for psql from your machine
```

`INTERNAL_API_KEY` must be a fresh value — `dev-internal-api-key` from appsettings must never
reach a deployed environment. It is the shared secret the Functions app uses to report export
status back to the API, and both apps get it from the same parameter.

## 3. First deploy — infrastructure only

> Sections 3–5 are optional. Once section 7 is wired up the pipeline does all of it itself,
> including the first run against an empty resource group. Do them by hand only to deploy
> without GitHub, or to debug a template change locally.

No image exists in ACR yet, so skip the apps on the first pass:

```bash
RG=rg-tasktracker-dev
az group create -n $RG -l westeurope

DEPLOY_APPS=false az deployment group create \
  -g $RG -f main.bicep -p main.dev.bicepparam
```

Record the outputs — `registryLoginServer`, `frontendUrl`, `apiUrl`, `stripeWebhookUrl`,
`spaRedirectUri`.

Go back to the app registration and add `spaRedirectUri` as the SPA redirect URI.

## 4. Build and push the images

```bash
ACR=$(az deployment group show -g $RG -n main --query properties.outputs.registryName.value -o tsv)
az acr login -n $ACR
SERVER="$ACR.azurecr.io"
TAG=$(git rev-parse --short HEAD)

cd ..
docker build -t $SERVER/tasktracker-api:$TAG        -f src/Backend/Presentation/Dockerfile .
docker build -t $SERVER/tasktracker-frontend:$TAG   -f src/Frontend/WebApp/Dockerfile .
docker build -t $SERVER/tasktracker-functions:$TAG  -f src/Microservices/TaskTracker.Functions/Dockerfile .
docker build -t $SERVER/tasktracker-migration:$TAG  -f src/Backend/Database/Dockerfile .
docker push $SERVER/tasktracker-api:$TAG
docker push $SERVER/tasktracker-frontend:$TAG
docker push $SERVER/tasktracker-functions:$TAG
docker push $SERVER/tasktracker-migration:$TAG
cd infra
```

## 5. Migrate, then deploy the apps

Order matters. The schema goes on while the API is still serving the old image, then the apps
roll onto the new one. `deployApps=false` only touches infrastructure and the migration job —
incremental mode leaves running apps alone.

```bash
# point the migration job at the new image
IMAGE_TAG=$TAG DEPLOY_APPS=false az deployment group create   -g $RG -f main.bicep -p main.dev.bicepparam

# apply the schema and wait for it
EXEC=$(az containerapp job start -g $RG -n job-migrate-dev --query name -o tsv)
az containerapp job execution show -g $RG -n job-migrate-dev   --job-execution-name $EXEC --query properties.status -o tsv

# only now roll the apps
IMAGE_TAG=$TAG az deployment group create -g $RG -f main.bicep -p main.dev.bicepparam
```

This is exactly what `.github/workflows/deploy-dev.yml` automates. See section 7.

## 6. Stripe test mode

`Subscription:Plans` already carries test-mode price IDs, so the products exist. What the
deployed environment needs is a webhook pointing at it.

1. Stripe dashboard, **Test mode on**.
2. Developers → Webhooks → Add endpoint → URL = the `stripeWebhookUrl` output
   (`https://<api-fqdn>/webhooks/stripe`).
3. Select exactly these three events — the only ones with handlers:
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
4. Copy the endpoint's **Signing secret** (`whsec_...`).
5. Redeploy with it, so the API can verify signatures:

```bash
export STRIPE_WEBHOOK_SECRET="whsec_..."
IMAGE_TAG=$TAG az deployment group create -g $RG -f main.bicep -p main.dev.bicepparam
```

Test card `4242 4242 4242 4242`, any future expiry, any CVC, any postcode.

## 7. GitHub Actions

Two workflows:

| Workflow | Trigger | Does |
|---|---|---|
| `ci.yml` | PR, push to main | builds, tests, compiles Bicep, builds all four images without pushing |
| `deploy-dev.yml` | push to main, manual | infra → images → migrate → release, then smoke-checks |

`ci.yml` needs no secrets at all — `az bicep build` is offline, so it works on forks.

`deploy-dev.yml` runs three jobs in order, and that order is what enforces the
schema-ahead-of-code rule:

| Job | Does | Why here |
|---|---|---|
| `infra` | deploys with `deployApps=false` | creates the registry before anything pushes to it, and moves the migration job to the new tag while the apps stay on the old one |
| `images` | builds and pushes four images in parallel | needs the registry from `infra`; skipped entirely on a rollback |
| `release` | runs DbUp, waits, then deploys with `deployApps=true` | the apps only move once the schema is already in place |

### 7.1 Deployer identity (OIDC, no stored password)

```bash
REPO="<owner>/<repo>"
az ad app create --display-name "TaskTracker GitHub deploy"
APP_ID=$(az ad app list --display-name "TaskTracker GitHub deploy" --query "[0].appId" -o tsv)
az ad sp create --id "$APP_ID"
```

Two federated credentials are required, not one. A job that declares `environment: dev` presents
a different subject than a job that does not, and the `deploy` job declares it:

```bash
az ad app federated-credential create --id "$APP_ID" --parameters "{
  \"name\": \"main-branch\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:${REPO}:ref:refs/heads/main\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"

az ad app federated-credential create --id "$APP_ID" --parameters "{
  \"name\": \"env-dev\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:${REPO}:environment:dev\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"
```

Missing the second one gives `AADSTS70021: No matching federated identity record found` on the
deploy job while the image job succeeds.

Two role assignments, also not one:

```bash
RG_ID=$(az group show -n "$RG" --query id -o tsv)
az role assignment create --assignee "$APP_ID" --role "Contributor" --scope "$RG_ID"
az role assignment create --assignee "$APP_ID"   --role "Role Based Access Control Administrator" --scope "$RG_ID"
```

Contributor cannot create role assignments, and the template creates two (AcrPull for the app
identity, Key Vault Secrets User). Without the second role the deployment fails on
`Microsoft.Authorization/roleAssignments/write`. `User Access Administrator` works too, and is
broader.

### 7.2 Repository configuration

The app's own Entra registration and the deployer are **different** registrations. The variable
names keep them apart — `AZURE_DEPLOY_*` is the deployer, `APP_AZURE_*` is what the running app
uses. Mixing them produces a login that succeeds and an app that rejects every token.

```bash
gh variable set AZURE_DEPLOY_CLIENT_ID   --body "$APP_ID"
gh variable set AZURE_DEPLOY_TENANT_ID   --body "$(az account show --query tenantId -o tsv)"
gh variable set AZURE_SUBSCRIPTION_ID    --body "$(az account show --query id -o tsv)"
gh variable set AZURE_RESOURCE_GROUP     --body "$RG"

gh variable set APP_AZURE_CLIENT_ID      --body "<app registration client id>"
gh variable set APP_AZURE_TENANT_ID      --body "<tenant id>"
gh variable set AZURE_OPENAI_ENDPOINT    --body "https://<your-aoai>.openai.azure.com"
gh variable set AZURE_OPENAI_DEPLOYMENT  --body "<chat deployment>"
gh variable set AZURE_AI_SEARCH_ENDPOINT --body "https://<your-search>.search.windows.net"
gh variable set AZURE_AI_SEARCH_INDEX    --body "<index name>"

gh secret set POSTGRES_ADMIN_PASSWORD
gh secret set STRIPE_SECRET_KEY
gh secret set STRIPE_WEBHOOK_SECRET
gh secret set INTERNAL_API_KEY
gh secret set AZURE_OPENAI_API_KEY
gh secret set AZURE_AI_SEARCH_API_KEY
```

The registry is deliberately not a variable: the pipeline reads it from its own deployment
output, so the first run works against an empty resource group. Sections 3–5 are the manual
equivalent, kept as a reference and an escape hatch.

Optional: repo Settings → Environments → `dev` → required reviewers, to gate deploys behind
an approval click.

### 7.3 Everyday use

Merge to `main`. That is the whole flow — images build in parallel, the schema migrates, the apps
roll, and the run summary prints the URLs.

**Roll back** without rebuilding: Actions → Deploy dev → Run workflow → put a previous short SHA
in `imageTag`. The image job is skipped and the apps move to that tag. Note this does **not**
revert the schema — DbUp is forward-only, so a rollback across a migration needs a new script.

`skipMigration` exists for the case where you know no script was added and want to skip a
two-minute job. Leave it off by default; running DbUp with nothing to do is harmless.

## Cost control

Stop Postgres between demos — it is the largest always-on line item:

```bash
az postgres flexible-server stop -g $RG -n <server-name>   # storage only while stopped
az postgres flexible-server start -g $RG -n <server-name>
```

Scale the apps to zero when idle (accepts a cold start):

```bash
az containerapp update -g $RG -n ca-api-dev --min-replicas 0
az containerapp update -g $RG -n ca-web-dev --min-replicas 0
```

Tear the whole thing down with `az group delete -n $RG --yes`. Key Vault has soft-delete on with
7-day retention, so reusing the same vault name inside a week needs `--purge` first.

## Known constraints

- **API is pinned to one replica.** The SignalR hubs keep per-instance connection state and the
  Hangfire recurring jobs would double-fire. Add a backplane (Azure SignalR, Free tier covers a
  demo) before raising `maxReplicas`.
- **Container Apps secrets are plain, not Key Vault references.** The vault is provisioned and
  seeded, but an RBAC assignment created in the same deployment has not propagated in time for
  the apps to read through it. Switch to `keyVaultUrl` references once the environment is stable.
- **Connection strings, not managed identity, for the data services.** The app reads
  `ConnectionStrings__*`; passwordless access would need `DefaultAzureCredential` wired into
  BlobModule, the Cosmos client and the Service Bus client first.
- **No VNet or private endpoints.** Everything is public with firewall rules, which is a dev
  trade-off, not a production posture.
