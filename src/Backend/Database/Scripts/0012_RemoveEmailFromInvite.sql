BEGIN;

DROP INDEX IF EXISTS "IX_WorkspaceInvites_WorkspaceId_Email";

ALTER TABLE "WorkspaceInvites"
DROP COLUMN IF EXISTS "Email";

COMMIT;