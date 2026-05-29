CREATE TABLE "Boards"
(
    "Id"          UUID PRIMARY KEY,
    "Name"        VARCHAR(100)             NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE
);

CREATE TABLE "Columns"
(
    "Id"          UUID PRIMARY KEY,
    "BoardId"     UUID                     NOT NULL,
    "Name"        VARCHAR(50)              NOT NULL,
    "Position"    INT                      NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "FK_Columns_Boards"
        FOREIGN KEY ("BoardId") REFERENCES "Boards" ("Id") ON DELETE CASCADE,

    CONSTRAINT "CK_Columns_Position" CHECK ("Position" >= 0)
);

CREATE INDEX "IX_Columns_BoardId" ON "Columns" ("BoardId");

CREATE TABLE "Tasks"
(
    "Id"          UUID PRIMARY KEY,
    "ColumnId"    UUID                     NOT NULL,
    "Title"       VARCHAR(200)             NOT NULL,
    "Description" TEXT                     NULL,
    "Position"    INT                      NOT NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID                     NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID                     NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID                     NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "FK_Tasks_Columns"
        FOREIGN KEY ("ColumnId") REFERENCES "Columns" ("Id") ON DELETE CASCADE,

    CONSTRAINT "CK_Tasks_Position" CHECK ("Position" >= 0)
);

CREATE INDEX "IX_Tasks_ColumnId" ON "Tasks" ("ColumnId");