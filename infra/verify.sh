#!/usr/bin/env bash
# What is actually deployed, and is it healthy?
#
#   ./infra/verify.sh [resource-group]
#
# Safe to run at any point: it only reads. Run it from Cloud Shell, or anywhere
# `az` is logged in to the right subscription.

set -uo pipefail
RG="${1:-rg-tasktracker-dev}"

pass() { printf '  \033[32mok\033[0m    %s\n' "$1"; }
fail() { printf '  \033[31mMISSING\033[0m %s\n' "$1"; MISSING=$((MISSING + 1)); }
warn() { printf '  \033[33mwarn\033[0m  %s\n' "$1"; }
head() { printf '\n\033[1m%s\033[0m\n' "$1"; }

MISSING=0

if ! az group show -n "$RG" -o none 2>/dev/null; then
  echo "Resource group '$RG' not found in the current subscription."
  az account show --query "{subscription:name, id:id}" -o table
  exit 1
fi

echo "Resource group: $RG   ($(az group show -n "$RG" --query location -o tsv))"

# ---------- infrastructure ----------
head "Infrastructure"
check_type() {
  local type="$1" label="$2"
  local name
  name=$(az resource list -g "$RG" --resource-type "$type" --query "[0].name" -o tsv 2>/dev/null)
  if [ -n "$name" ]; then pass "$label  ($name)"; else fail "$label  [$type]"; fi
}

check_type Microsoft.OperationalInsights/workspaces        "Log Analytics"
check_type Microsoft.Insights/components                   "Application Insights"
check_type Microsoft.ContainerRegistry/registries          "Container registry"
check_type Microsoft.ManagedIdentity/userAssignedIdentities "Managed identity"
check_type Microsoft.Storage/storageAccounts               "Storage account"
check_type Microsoft.DBforPostgreSQL/flexibleServers       "Postgres"
check_type Microsoft.ServiceBus/namespaces                 "Service Bus"
check_type Microsoft.Communication/communicationServices   "Communication Services"
check_type Microsoft.KeyVault/vaults                       "Key Vault"
check_type Microsoft.App/managedEnvironments               "Container Apps environment"

# ---------- data-plane topology ----------
head "Inside those resources"
STORAGE=$(az resource list -g "$RG" --resource-type Microsoft.Storage/storageAccounts --query "[0].name" -o tsv)
if [ -n "$STORAGE" ]; then
  for c in avatars attachments board-archives; do
    if az storage container show --account-name "$STORAGE" --name "$c" --auth-mode login -o none 2>/dev/null; then
      pass "blob container $c"
    else
      warn "blob container $c not readable (needs Storage Blob Data Reader, or it is absent)"
    fi
  done
fi

PSQL=$(az resource list -g "$RG" --resource-type Microsoft.DBforPostgreSQL/flexibleServers --query "[0].name" -o tsv)
if [ -n "$PSQL" ]; then
  STATE=$(az postgres flexible-server show -g "$RG" -n "$PSQL" --query state -o tsv 2>/dev/null)
  [ "$STATE" = "Ready" ] && pass "Postgres state: $STATE" || warn "Postgres state: ${STATE:-unknown}"
  az postgres flexible-server db show -g "$RG" -s "$PSQL" -d TaskTrackerDb -o none 2>/dev/null \
    && pass "database TaskTrackerDb" || fail "database TaskTrackerDb"
fi

SB=$(az resource list -g "$RG" --resource-type Microsoft.ServiceBus/namespaces --query "[0].name" -o tsv)
if [ -n "$SB" ]; then
  az servicebus queue show -g "$RG" --namespace-name "$SB" -n board-archiving-queue -o none 2>/dev/null \
    && pass "queue board-archiving-queue" || fail "queue board-archiving-queue"
fi

# ---------- applications ----------
head "Applications"
for app in ca-api-dev ca-web-dev ca-func-dev; do
  STATE=$(az containerapp show -g "$RG" -n "$app" --query "properties.provisioningState" -o tsv 2>/dev/null)
  if [ -z "$STATE" ]; then
    warn "$app not deployed yet (expected until the release job runs)"
  elif [ "$STATE" = "Succeeded" ]; then
    IMG=$(az containerapp show -g "$RG" -n "$app" \
          --query "properties.template.containers[0].image" -o tsv 2>/dev/null | sed 's#.*/##')
    pass "$app  ($IMG)"
  else
    fail "$app provisioning state: $STATE"
  fi
done

JOB=$(az containerapp job show -g "$RG" -n job-migrate-dev --query name -o tsv 2>/dev/null)
if [ -n "$JOB" ]; then
  LAST=$(az containerapp job execution list -g "$RG" -n job-migrate-dev \
         --query "sort_by(@, &properties.startTime)[-1].{s:properties.status, t:properties.startTime}" -o tsv 2>/dev/null)
  pass "migration job exists  (last run: ${LAST:-never})"
else
  fail "migration job job-migrate-dev"
fi

# ---------- endpoints ----------
head "Endpoints"
for pair in "ca-web-dev:frontend" "ca-api-dev:api"; do
  app="${pair%%:*}"; label="${pair##*:}"
  FQDN=$(az containerapp show -g "$RG" -n "$app" --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null)
  if [ -n "$FQDN" ]; then
    CODE=$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "https://$FQDN" || echo 000)
    # 401 from the API means it is up and refusing an unauthenticated call.
    case "$CODE" in
      200|401|403) pass "$label https://$FQDN  (HTTP $CODE)" ;;
      *)           warn "$label https://$FQDN  (HTTP $CODE)" ;;
    esac
  fi
done

# ---------- deployment history ----------
head "Recent deployments"
az deployment group list -g "$RG" \
  --query "sort_by(@, &properties.timestamp)[-5:].{name:name, state:properties.provisioningState, at:properties.timestamp}" \
  -o table 2>/dev/null

head "Failed operations, if any"
FAILED=$(az deployment group list -g "$RG" \
  --query "[?properties.provisioningState=='Failed'].name" -o tsv 2>/dev/null | tail -3)
if [ -z "$FAILED" ]; then
  echo "  none"
else
  for d in $FAILED; do
    echo "  -- $d"
    az deployment operation group list -g "$RG" -n "$d" \
      --query "[?properties.provisioningState=='Failed'].properties.statusMessage" -o json 2>/dev/null | head -40
  done
fi

head "Summary"
if [ "$MISSING" -eq 0 ]; then
  echo "  Nothing missing."
else
  echo "  $MISSING expected resource(s) missing."
fi
