BEGIN;

ALTER TABLE "Boards"
    ADD COLUMN "IsArchived" BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN "ArchivedAt" TIMESTAMP WITH TIME ZONE NULL;

ALTER TABLE "Boards"
    ADD CONSTRAINT "CK_Boards_Archive" CHECK (
        ("IsArchived" = false AND "ArchivedAt" IS NULL) OR
        ("IsArchived" = true AND "ArchivedAt" IS NOT NULL)
        );

COMMIT;