BEGIN;

ALTER TABLE "Comments"
    ADD COLUMN "AuthorId" uuid NOT NULL;

ALTER TABLE "Comments"
    ADD CONSTRAINT "FK_Comments_Users_AuthorId"
        FOREIGN KEY ("AuthorId") REFERENCES "Users" ("Id") ON DELETE RESTRICT;


ALTER TABLE "Attachments"
    ADD COLUMN "UploadedById" uuid NOT NULL;

ALTER TABLE "Attachments"
    ADD CONSTRAINT "FK_Attachments_Users_UploadedById"
        FOREIGN KEY ("UploadedById") REFERENCES "Users" ("Id") ON DELETE RESTRICT;

COMMIT;