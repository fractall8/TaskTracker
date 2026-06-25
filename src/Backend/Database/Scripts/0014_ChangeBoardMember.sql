DO
$$
BEGIN
    IF
EXISTS (
        SELECT 1 
        FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_BoardMembers_Users_UserId' 
        AND table_name = 'BoardMembers'
    ) THEN
ALTER TABLE "BoardMembers" DROP CONSTRAINT "FK_BoardMembers_Users_UserId";
END IF;
END $$;

DROP INDEX IF EXISTS "IX_BoardMembers_BoardId_UserId";

DO
$$
BEGIN
    IF
NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'BoardMembers' 
        AND column_name = 'WorkspaceMemberId'
    ) THEN
DELETE
FROM "BoardMembers";

ALTER TABLE "BoardMembers" DROP COLUMN IF EXISTS "UserId";

ALTER TABLE "BoardMembers"
    ADD "WorkspaceMemberId" uuid NOT NULL;
END IF;
END $$;

DO
$$
BEGIN
    IF
NOT EXISTS (
        SELECT 1 
        FROM information_schema.table_constraints 
        WHERE constraint_name = 'FK_BoardMembers_WorkspaceMembers_WorkspaceMemberId' 
        AND table_name = 'BoardMembers'
    ) THEN
ALTER TABLE "BoardMembers"
    ADD CONSTRAINT "FK_BoardMembers_WorkspaceMembers_WorkspaceMemberId" FOREIGN KEY ("WorkspaceMemberId") REFERENCES "WorkspaceMembers" ("Id") ON DELETE CASCADE;
END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_BoardMembers_BoardId_WorkspaceMemberId" ON "BoardMembers" ("BoardId", "WorkspaceMemberId") WHERE "IsDeleted" = FALSE;
CREATE INDEX IF NOT EXISTS "IX_BoardMembers_WorkspaceMemberId" ON "BoardMembers" ("WorkspaceMemberId");