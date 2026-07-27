BEGIN;

ALTER TABLE "Users" ADD COLUMN "AcsCommunicationUserId" character varying(255) NULL;

ALTER TABLE "Users"
    ADD CONSTRAINT "CK_Users_AcsCommunicationUserId_NotEmpty" CHECK ("AcsCommunicationUserId" IS NULL OR btrim("AcsCommunicationUserId") <> '');

CREATE UNIQUE INDEX "IX_Users_AcsCommunicationUserId"
    ON "Users" ("AcsCommunicationUserId") WHERE "AcsCommunicationUserId" IS NOT NULL;

COMMIT;
