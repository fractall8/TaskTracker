BEGIN;

CREATE TABLE "Tags"
(
    "Id"          uuid                     NOT NULL,
    "WorkspaceId" uuid                     NOT NULL,
    "Name"        character varying(30)    NOT NULL,
    "Color"       character varying(7)     NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "PK_Tags" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Tags_Name_NotEmpty" CHECK (btrim("Name") <> ''),
    CONSTRAINT "CK_Tags_Color_Hex" CHECK ("Color" ~ '^#[0-9A-Fa-f]{6}$')
);

CREATE TABLE "TaskTags"
(
    "Id"          uuid                     NOT NULL,
    "TaskId"      uuid                     NOT NULL,
    "TagId"       uuid                     NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "PK_TaskTags" PRIMARY KEY ("Id")
);

-- A tag belongs to its workspace's lifecycle, matching Boards.
ALTER TABLE "Tags"
    ADD CONSTRAINT "FK_Tags_Workspaces_WorkspaceId" FOREIGN KEY ("WorkspaceId") REFERENCES "Workspaces" ("Id") ON DELETE CASCADE;

ALTER TABLE "TaskTags"
    ADD CONSTRAINT "FK_TaskTags_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE CASCADE;

ALTER TABLE "TaskTags"
    ADD CONSTRAINT "FK_TaskTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE;

-- Case-insensitive uniqueness per workspace (EPIC 4 Decision 6), so "bug" and "Bug" cannot coexist.
-- Partial, so deleting a tag frees its name for reuse.
CREATE UNIQUE INDEX "IX_Tags_WorkspaceId_Name"
    ON "Tags" ("WorkspaceId", lower("Name")) WHERE "IsDeleted" = FALSE;

-- A tag is attached to a task at most once.
CREATE UNIQUE INDEX "IX_TaskTags_TaskId_TagId"
    ON "TaskTags" ("TaskId", "TagId") WHERE "IsDeleted" = FALSE;

-- Backs "which tasks carry this tag", used by the board tag filter.
CREATE INDEX "IX_TaskTags_TagId"
    ON "TaskTags" ("TagId") WHERE "IsDeleted" = FALSE;

COMMIT;
