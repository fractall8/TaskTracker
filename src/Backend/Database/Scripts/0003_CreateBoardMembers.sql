CREATE TABLE "BoardMembers"
(
    "Id"          UUID PRIMARY KEY,
    "BoardId"     UUID                     NOT NULL REFERENCES "Boards" ("Id") ON DELETE CASCADE,

    "UserId"      UUID                     NOT NULL REFERENCES "Users" ("Id") ON DELETE RESTRICT,

    "JoinedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Role"        SMALLINT                 NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedById" UUID,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE,
    "UpdatedById" UUID,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE,
    "DeletedById" UUID,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE
);

CREATE UNIQUE INDEX "IX_BoardMembers_BoardId_UserId" ON "BoardMembers" ("BoardId", "UserId");

CREATE INDEX "IX_BoardMembers_UserId" ON "BoardMembers" ("UserId");