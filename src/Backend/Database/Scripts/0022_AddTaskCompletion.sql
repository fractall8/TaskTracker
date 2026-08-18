BEGIN;

ALTER TABLE "Tasks" ADD COLUMN "IsCompleted" BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE "Tasks" ADD COLUMN "CompletedAt" TIMESTAMP WITH TIME ZONE NULL;
ALTER TABLE "Tasks" ADD COLUMN "CompletedById" UUID NULL;

ALTER TABLE "Tasks"
    ADD CONSTRAINT "FK_Tasks_CompletedBy"
        FOREIGN KEY ("CompletedById") REFERENCES "Users" ("Id") ON DELETE SET NULL;

-- The three columns are one fact. Enforced here so a bad write fails at the database, not only in C#.
ALTER TABLE "Tasks"
    ADD CONSTRAINT "CK_Tasks_Completion_Consistent"
        CHECK (("IsCompleted" = FALSE AND "CompletedAt" IS NULL AND "CompletedById" IS NULL)
            OR ("IsCompleted" = TRUE AND "CompletedAt" IS NOT NULL));

-- Overdue and due-soon queries always filter on completion first (EPIC 4 Decision 4).
CREATE INDEX "IX_Tasks_IsCompleted_DueDate"
    ON "Tasks" ("IsCompleted", "DueDate") WHERE "IsDeleted" = FALSE;

COMMIT;
