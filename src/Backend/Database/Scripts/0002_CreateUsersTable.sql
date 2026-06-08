CREATE TABLE "Users"
(
    "Id"              UUID PRIMARY KEY,
    "AzureAdObjectId" UUID UNIQUE              NOT NULL,
    "Email"           VARCHAR(255)             NOT NULL,
    "DisplayName"     VARCHAR(255)             NOT NULL,

    "CreatedAt"       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById"     UUID NULL,
    "UpdatedAt"       TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById"     UUID NULL,
    "DeletedAt"       TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById"     UUID NULL,
    "IsDeleted"       BOOLEAN                  NOT NULL DEFAULT FALSE
);

CREATE INDEX "IX_Users_AzureAdObjectId" ON "Users" ("AzureAdObjectId");