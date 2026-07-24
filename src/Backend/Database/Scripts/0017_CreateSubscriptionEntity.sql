BEGIN;

CREATE TABLE "Subscriptions"
(
    "Id"                   uuid                     NOT NULL,
    "UserId"               uuid                     NOT NULL,
    "PlanId"               character varying(64)    NOT NULL,
    "StripeCustomerId"     character varying(255)   NOT NULL,
    "StripeSubscriptionId" character varying(255)   NOT NULL,
    "Status"               character varying(32)    NOT NULL,
    "CurrentPeriodStartAt" timestamp with time zone,
    "CurrentPeriodEndAt"   timestamp with time zone,
    "CancelAtPeriodEnd"    boolean                  NOT NULL DEFAULT FALSE,

    "CreatedAt"            TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedById"          UUID NULL,
    "UpdatedAt"            TIMESTAMP WITH TIME ZONE NULL,
    "UpdatedById"          UUID NULL,
    "DeletedAt"            TIMESTAMP WITH TIME ZONE NULL,
    "DeletedById"          UUID NULL,
    "IsDeleted"            BOOLEAN                  NOT NULL DEFAULT FALSE,

    CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Subscriptions_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_Subscriptions_StripeSubscriptionId"
    ON "Subscriptions" ("StripeSubscriptionId");

CREATE UNIQUE INDEX "IX_Subscriptions_UserId_Billable"
    ON "Subscriptions" ("UserId") WHERE "Status" IN ('active', 'trialing', 'past_due');

CREATE INDEX "IX_Subscriptions_UserId"
    ON "Subscriptions" ("UserId");

CREATE INDEX "IX_Subscriptions_StripeCustomerId"
    ON "Subscriptions" ("StripeCustomerId");

COMMIT;