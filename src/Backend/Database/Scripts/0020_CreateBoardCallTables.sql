BEGIN;

CREATE TABLE "BoardCalls"
(
    "Id"              uuid                     NOT NULL,
    "BoardId"         uuid                     NOT NULL,
    "StartedByUserId" uuid                     NOT NULL,
    "AcsRoomId"       character varying(128)   NOT NULL,
    "StartedAt"       timestamp with time zone NOT NULL,
    "EndedAt"         timestamp with time zone NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "PK_BoardCalls" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_BoardCalls_EndedAfterStarted" CHECK ("EndedAt" IS NULL OR "EndedAt" >= "StartedAt"),
    CONSTRAINT "CK_BoardCalls_AcsRoomId_NotEmpty" CHECK (btrim("AcsRoomId") <> '')
);

CREATE TABLE "BoardCallParticipants"
(
    "Id"          uuid                     NOT NULL,
    "BoardCallId" uuid                     NOT NULL,
    "UserId"      uuid                     NOT NULL,
    "JoinedAt"    timestamp with time zone NOT NULL,
    "LeftAt"      timestamp with time zone NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "PK_BoardCallParticipants" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_BoardCallParticipants_LeftAfterJoined" CHECK ("LeftAt" IS NULL OR "LeftAt" >= "JoinedAt")
);

-- BoardCalls belongs to its Board's lifecycle -- cascades on board delete, matching Columns/Tasks.
ALTER TABLE "BoardCalls"
    ADD CONSTRAINT "FK_BoardCalls_Boards_BoardId" FOREIGN KEY ("BoardId") REFERENCES "Boards" ("Id") ON DELETE CASCADE;

ALTER TABLE "BoardCalls"
    ADD CONSTRAINT "FK_BoardCalls_Users_StartedByUserId" FOREIGN KEY ("StartedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

-- BoardCallParticipants belongs to its BoardCall's lifecycle -- cascades on call delete.
ALTER TABLE "BoardCallParticipants"
    ADD CONSTRAINT "FK_BoardCallParticipants_BoardCalls_BoardCallId" FOREIGN KEY ("BoardCallId") REFERENCES "BoardCalls" ("Id") ON DELETE CASCADE;

ALTER TABLE "BoardCallParticipants"
    ADD CONSTRAINT "FK_BoardCallParticipants_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

-- Only one active (unended) call per board at a time -- race-safe enforcement of AC #1.
CREATE UNIQUE INDEX "IX_BoardCalls_BoardId_Active"
    ON "BoardCalls" ("BoardId") WHERE "EndedAt" IS NULL;

-- One ACS Room maps 1:1 to one BoardCall.
CREATE UNIQUE INDEX "IX_BoardCalls_AcsRoomId"
    ON "BoardCalls" ("AcsRoomId");

-- Only one active (unleft) participant row per user per call -- makes rejoin-after-leave idempotent.
CREATE UNIQUE INDEX "IX_BoardCallParticipants_BoardCallId_UserId_Active"
    ON "BoardCallParticipants" ("BoardCallId", "UserId") WHERE "LeftAt" IS NULL;

CREATE INDEX "IX_BoardCallParticipants_BoardCallId"
    ON "BoardCallParticipants" ("BoardCallId");

COMMIT;
