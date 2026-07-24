BEGIN;

DROP INDEX IF EXISTS "IX_Subscriptions_UserId_Billable";

ALTER TABLE "Subscriptions"
    ADD COLUMN "WorkspaceId" uuid NOT NULL;

ALTER TABLE "Subscriptions"
    ADD CONSTRAINT "FK_Subscriptions_Workspaces_WorkspaceId"
        FOREIGN KEY ("WorkspaceId") REFERENCES "Workspaces" ("Id") ON DELETE RESTRICT;

CREATE UNIQUE INDEX "IX_Subscriptions_WorkspaceId"
    ON "Subscriptions" ("WorkspaceId") WHERE "Status" IN ('active', 'trialing', 'past_due');

COMMIT;