CREATE TABLE "Comments"
(
    "Id"          UUID PRIMARY KEY,
    "Text"        VARCHAR(2000)            NOT NULL,
    "TaskId"      UUID                     NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "FK_Comments_Tasks_TaskId" FOREIGN KEY ("TaskId")
        REFERENCES "Tasks" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Comments_TaskId" ON "Comments" ("TaskId");