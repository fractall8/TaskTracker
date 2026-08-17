BEGIN;

-- Tag colours move from free hex to a fixed palette so each one can carry a hand-picked light/dark
-- chip pair. An arbitrary hex cannot: a pale pick is unreadable in light mode whatever the text colour.

ALTER TABLE "Tags" DROP CONSTRAINT "CK_Tags_Color_Hex";

ALTER TABLE "Tags" ALTER COLUMN "Color" TYPE character varying(16);

-- Anything already stored is a hex from the old colour picker; the palette has no equivalent, so it
-- falls back to the neutral. "slate" was also the old default.
UPDATE "Tags"
SET "Color" = 'slate'
WHERE "Color" NOT IN ('violet', 'sky', 'teal', 'amber', 'rose', 'slate');

ALTER TABLE "Tags"
    ADD CONSTRAINT "CK_Tags_Color_Palette"
        CHECK ("Color" IN ('violet', 'sky', 'teal', 'amber', 'rose', 'slate'));

COMMIT;
