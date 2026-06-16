CREATE TABLE "Attachments"
(
    "Id"          UUID PRIMARY KEY,
    "FileName"    VARCHAR(255)             NOT NULL,
    "FileUrl"     VARCHAR(2000)            NOT NULL,
    "SizeInBytes" BIGINT                   NOT NULL,
    "ContentType" VARCHAR(100)             NOT NULL,
    "TaskId"      UUID                     NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "FK_Attachments_Tasks_TaskId" FOREIGN KEY ("TaskId")
        REFERENCES "Tasks" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Attachments_TaskId" ON "Attachments" ("TaskId");