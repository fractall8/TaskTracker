CREATE TABLE "Workspaces"
(
    "Id"          UUID PRIMARY KEY,
    "Name"        VARCHAR(100)             NOT NULL,
    "Description" VARCHAR(500)             NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE
);

CREATE TABLE "WorkspaceMembers"
(
    "Id"          UUID PRIMARY KEY,
    "WorkspaceId" UUID                     NOT NULL REFERENCES "Workspaces" ("Id") ON DELETE CASCADE,
    "UserId"      UUID                     NOT NULL REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    "JoinedAt"    TIMESTAMP WITH TIME ZONE NOT NULL,
    "Role"        INTEGER                  NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE
);

CREATE UNIQUE INDEX "IX_WorkspaceMembers_WorkspaceId_UserId" ON "WorkspaceMembers" ("WorkspaceId", "UserId");
CREATE INDEX "IX_WorkspaceMembers_UserId" ON "WorkspaceMembers" ("UserId");

CREATE TABLE "WorkspaceInvites"
(
    "Id"          UUID PRIMARY KEY,
    "WorkspaceId" UUID                     NOT NULL REFERENCES "Workspaces" ("Id") ON DELETE CASCADE,
    "Email"       VARCHAR(256)             NOT NULL,
    "Token"       VARCHAR(64)              NOT NULL,
    "ExpiresAt"   TIMESTAMP WITH TIME ZONE NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE
);

CREATE UNIQUE INDEX "IX_WorkspaceInvites_Token" ON "WorkspaceInvites" ("Token");
CREATE INDEX "IX_WorkspaceInvites_WorkspaceId" ON "WorkspaceInvites" ("WorkspaceId");

ALTER TABLE "Boards"
    ADD COLUMN "WorkspaceId" UUID NOT NULL REFERENCES "Workspaces" ("Id") ON DELETE CASCADE;
