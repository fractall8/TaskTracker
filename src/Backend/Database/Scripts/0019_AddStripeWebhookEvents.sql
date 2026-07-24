BEGIN;

CREATE TABLE "StripeWebhookEvents"
(
    "Id"          uuid                     NOT NULL,
    "EventId"     character varying(255)   NOT NULL,
    "EventType"   character varying(128)   NOT NULL,
    "ReceivedAt"  timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone NULL,
    "LastError"   text NULL,

    "CreatedAt"   TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById" UUID NULL,
    "UpdatedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById" UUID NULL,
    "DeletedAt"   TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById" UUID NULL,
    "IsDeleted"   BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "PK_StripeWebhookEvents" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_StripeWebhookEvents_EventId"
    ON "StripeWebhookEvents" ("EventId");

CREATE INDEX "IX_StripeWebhookEvents_ReceivedAt_Unprocessed"
    ON "StripeWebhookEvents" ("ReceivedAt") WHERE "ProcessedAt" IS NULL;

COMMIT;