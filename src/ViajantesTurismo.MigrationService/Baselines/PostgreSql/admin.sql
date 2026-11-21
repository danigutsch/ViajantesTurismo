CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'disposition_hold') THEN
        CREATE SCHEMA disposition_hold;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'messaging') THEN
        CREATE SCHEMA messaging;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'branding_projection') THEN
        CREATE SCHEMA branding_projection;
    END IF;
END $EF$;

CREATE EXTENSION IF NOT EXISTS citext;

DO $$
BEGIN
    IF (SELECT datctype FROM pg_database WHERE datname = current_database())
        NOT IN ('C.UTF-8', 'C.utf8', 'en_US.UTF-8', 'en_US.utf8') THEN
        RAISE EXCEPTION 'Unsupported PostgreSQL LC_CTYPE for customer email uniqueness.';
    END IF;
END;
$$;

CREATE TABLE "Customers" (
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Customers" PRIMARY KEY ("Id")
);

CREATE TABLE "DocumentAuditPurgeRecords" (
    "Id" uuid NOT NULL,
    "ActorId" character varying(128) NOT NULL,
    "CorrelationId" character varying(128) NOT NULL,
    "PurgedCount" integer NOT NULL,
    "HeldCount" integer NOT NULL DEFAULT 0,
    "RetentionCutoffUtc" timestamp with time zone NOT NULL,
    "Outcome" text NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DocumentAuditPurgeRecords" PRIMARY KEY ("Id")
);

CREATE TABLE "DocumentAuditRecords" (
    "Id" uuid NOT NULL,
    "ActorId" character varying(128) NOT NULL,
    "DocumentId" uuid,
    "BookingId" uuid,
    "DocumentRevision" integer,
    "Operation" text NOT NULL,
    "Outcome" text NOT NULL,
    "ReasonCode" text NOT NULL,
    "CorrelationId" character varying(128) NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "HoldId" uuid,
    "HoldBasis" character varying(64),
    "RetentionExpiresAt" timestamp with time zone NOT NULL,
    "DocumentLineageId" uuid,
    CONSTRAINT "PK_DocumentAuditRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_DocumentAuditRecords_DocumentLineageStamped" CHECK ("DocumentId" IS NULL OR "DocumentLineageId" IS NOT NULL)
);

CREATE TABLE "DocumentLineages" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "Type" text NOT NULL,
    "Audience" text NOT NULL,
    "HighestFinalizedRevision" integer NOT NULL,
    "HighestRevision" integer NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_DocumentLineages" PRIMARY KEY ("Id")
);

CREATE TABLE "DocumentRetentionPurgeRecords" (
    "Id" uuid NOT NULL,
    "Kind" text NOT NULL,
    "PurgedCount" integer NOT NULL,
    "HeldCount" integer NOT NULL,
    "Outcome" text NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DocumentRetentionPurgeRecords" PRIMARY KEY ("Id")
);

CREATE TABLE disposition_hold.holds (
    "Id" uuid NOT NULL,
    "Scope" character varying(193) NOT NULL,
    "Basis" character varying(48) NOT NULL,
    "PurposeCode" character varying(128) NOT NULL,
    "AuthorityReference" character varying(128) NOT NULL,
    "PlacedBy" character varying(128) NOT NULL,
    "PlacedAt" timestamp with time zone NOT NULL,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "ReviewDueAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone,
    "ReleasedBy" character varying(128),
    "ReleasedAt" timestamp with time zone,
    "ReleaseReasonCode" character varying(128),
    CONSTRAINT "PK_holds" PRIMARY KEY ("Id")
);

CREATE TABLE messaging.idempotency_keys (
    "Scope" character varying(200) NOT NULL,
    "Key" character varying(255) NOT NULL,
    "State" character varying(32) NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "ResultFingerprint" character varying(512),
    "RetentionOwner" character varying(128),
    CONSTRAINT "PK_idempotency_keys" PRIMARY KEY ("Scope", "Key")
);

CREATE TABLE branding_projection.inbox_messages (
    "EventId" uuid NOT NULL,
    "SourceEpoch" uuid NOT NULL,
    "SourceRevision" bigint NOT NULL,
    "Outcome" character varying(16) NOT NULL,
    "ReasonCode" character varying(64),
    "ProcessedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_inbox_messages" PRIMARY KEY ("EventId")
);

CREATE TABLE messaging.outbox_messages (
    "Id" uuid NOT NULL,
    "EnqueuedAt" timestamp with time zone NOT NULL,
    "PublishedAt" timestamp with time zone,
    "PublishAttempts" integer NOT NULL,
    "LastPublishAttemptAt" timestamp with time zone,
    "NextPublishAttemptAt" timestamp with time zone,
    "LastPublishError" character varying(2000),
    "ClaimedBy" character varying(100),
    "ClaimedUntil" timestamp with time zone,
    "EnvelopeSpec" character varying(100) NOT NULL,
    "EnvelopeSpecVersion" character varying(20) NOT NULL,
    "EventId" character varying(200) NOT NULL,
    "Source" character varying(2048) NOT NULL,
    "EventType" character varying(200) NOT NULL,
    "EventVersion" integer,
    "Time" timestamp with time zone,
    "Subject" character varying(500),
    "DataContentType" character varying(200),
    "DataSchema" character varying(2048),
    "Payload" text,
    "PayloadEncoding" character varying(20) NOT NULL,
    "ExtensionAttributesJson" text,
    CONSTRAINT "PK_outbox_messages" PRIMARY KEY ("Id")
);

CREATE TABLE "PaymentAccounts" (
    "Id" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "Status" text NOT NULL,
    "Revision" integer NOT NULL,
    "ProtectedState" bytea NOT NULL,
    CONSTRAINT "PK_PaymentAccounts" PRIMARY KEY ("Id")
);

CREATE TABLE "PaymentAudits" (
    "Id" uuid NOT NULL,
    "Sequence" bigint GENERATED ALWAYS AS IDENTITY,
    "ResourceId" uuid NOT NULL,
    "TargetId" uuid,
    "Revision" bigint NOT NULL,
    "ActorId" character varying(256) NOT NULL,
    "OccurredAt" timestamp with time zone NOT NULL,
    "Operation" character varying(64) NOT NULL,
    "Reason" character varying(64) NOT NULL,
    "SelfApproved" boolean NOT NULL,
    CONSTRAINT "PK_PaymentAudits" PRIMARY KEY ("Id")
);

CREATE TABLE branding_projection.recovery (
    "Id" uuid NOT NULL,
    "Status" character varying(16) NOT NULL,
    "Reason" character varying(256),
    "Since" timestamp with time zone,
    "AuthorizedEventId" uuid,
    "AuthorizedEpoch" uuid,
    "AuthorizedRevision" bigint,
    "AuthorizationReason" character varying(256),
    "IncidentReference" character varying(256),
    "AuthorizedBy" character varying(256),
    "AuthorizedAt" timestamp with time zone,
    CONSTRAINT "PK_recovery" PRIMARY KEY ("Id")
);

CREATE TABLE branding_projection.recovery_audit (
    "Id" uuid NOT NULL,
    "Kind" character varying(32) NOT NULL,
    "OldEpoch" uuid,
    "OldRevision" bigint,
    "NewEpoch" uuid NOT NULL,
    "NewRevision" bigint NOT NULL,
    "EventId" uuid NOT NULL,
    "Reason" character varying(256) NOT NULL,
    "IncidentReference" character varying(256) NOT NULL,
    "Actor" character varying(256) NOT NULL,
    "RecordedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_recovery_audit" PRIMARY KEY ("Id")
);

CREATE TABLE branding_projection.settings (
    "Id" uuid NOT NULL,
    "SourceEpoch" uuid NOT NULL,
    "SourceRevision" bigint NOT NULL,
    "BrandName" character varying(120) NOT NULL,
    "PrimaryColor" character varying(7) NOT NULL,
    "AccentColor" character varying(7) NOT NULL,
    "BackgroundColor" character varying(7) NOT NULL,
    "TextColor" character varying(7) NOT NULL,
    "HeadingFontFamily" character varying(120) NOT NULL,
    "BodyFontFamily" character varying(120) NOT NULL,
    "LogoUri" character varying(2048),
    "LogoSha256" character varying(64),
    "LogoContentType" character varying(64),
    "LogoContent" bytea,
    CONSTRAINT "PK_settings" PRIMARY KEY ("Id")
);

CREATE TABLE "Tours" (
    "Id" uuid NOT NULL,
    "Identifier" character varying(64) NOT NULL,
    "Name" character varying(128) NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL,
    "Price" numeric NOT NULL,
    "DoubleRoomSupplementPrice" numeric NOT NULL,
    "RegularBikePrice" numeric NOT NULL,
    "EBikePrice" numeric NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "MinCustomers" integer NOT NULL,
    "MaxCustomers" integer NOT NULL,
    "IncludedServices" text[] NOT NULL,
    CONSTRAINT "PK_Tours" PRIMARY KEY ("Id")
);

CREATE TABLE messaging.transport_messages (
    "Id" uuid NOT NULL,
    "ConsumerName" character varying(200) NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    "ProcessedAt" timestamp with time zone,
    "ConsumeAttempts" integer NOT NULL,
    "LastConsumeAttemptAt" timestamp with time zone,
    "NextConsumeAttemptAt" timestamp with time zone,
    "LastConsumeError" character varying(2000),
    "PoisonedAt" timestamp with time zone,
    "ClaimedBy" character varying(100),
    "ClaimedUntil" timestamp with time zone,
    "EnvelopeSpec" character varying(100) NOT NULL,
    "EnvelopeSpecVersion" character varying(20) NOT NULL,
    "EventId" character varying(200) NOT NULL,
    "Source" character varying(2048) NOT NULL,
    "EventType" character varying(200) NOT NULL,
    "EventVersion" integer,
    "Time" timestamp with time zone,
    "Subject" character varying(500),
    "DataContentType" character varying(200),
    "DataSchema" character varying(2048),
    "Payload" text,
    "PayloadEncoding" character varying(20) NOT NULL,
    "ExtensionAttributesJson" text,
    CONSTRAINT "PK_transport_messages" PRIMARY KEY ("Id")
);

CREATE TABLE "CustomerAccommodationPreferences" (
    "CustomerId" uuid NOT NULL,
    "RoomType" integer NOT NULL,
    "BedType" integer NOT NULL,
    "CompanionId" uuid,
    CONSTRAINT "PK_CustomerAccommodationPreferences" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerAccommodationPreferences_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CustomerAddress" (
    "CustomerId" uuid NOT NULL,
    "Street" text NOT NULL,
    "Complement" text,
    "Neighborhood" text NOT NULL,
    "PostalCode" text NOT NULL,
    "City" text NOT NULL,
    "State" text NOT NULL,
    "Country" character(2) NOT NULL,
    CONSTRAINT "PK_CustomerAddress" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerAddress_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CustomerContactInfo" (
    "CustomerId" uuid NOT NULL,
    "Email" citext NOT NULL,
    "Mobile" character varying(16) NOT NULL,
    "Instagram" text,
    "Facebook" text,
    CONSTRAINT "PK_CustomerContactInfo" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerContactInfo_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CustomerEmergencyContact" (
    "CustomerId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Mobile" character varying(16) NOT NULL,
    CONSTRAINT "PK_CustomerEmergencyContact" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerEmergencyContact_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CustomerIdentificationInfo" (
    "CustomerId" uuid NOT NULL,
    "NationalId" text NOT NULL,
    "IdNationality" text NOT NULL,
    CONSTRAINT "PK_CustomerIdentificationInfo" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerIdentificationInfo_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CustomerMedicalInfo" (
    "CustomerId" uuid NOT NULL,
    "Allergies" text,
    "AdditionalInfo" text,
    CONSTRAINT "PK_CustomerMedicalInfo" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerMedicalInfo_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CustomerPersonalInfo" (
    "CustomerId" uuid NOT NULL,
    "FirstName" text NOT NULL,
    "LastName" text NOT NULL,
    "Gender" text NOT NULL,
    "BirthDate" date NOT NULL,
    "Nationality" text NOT NULL,
    "Occupation" text NOT NULL,
    CONSTRAINT "PK_CustomerPersonalInfo" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerPersonalInfo_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CustomerPhysicalInfo" (
    "CustomerId" uuid NOT NULL,
    "WeightKg" numeric NOT NULL,
    "HeightCentimeters" integer NOT NULL,
    "BikeType" integer NOT NULL,
    CONSTRAINT "PK_CustomerPhysicalInfo" PRIMARY KEY ("CustomerId"),
    CONSTRAINT "FK_CustomerPhysicalInfo_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentDrafts" (
    "Id" uuid NOT NULL,
    "DocumentLineageId" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "Type" text NOT NULL,
    "Audience" text NOT NULL,
    "TemplateId" character varying(128) NOT NULL,
    "TemplateVersion" character varying(64) NOT NULL,
    "Revision" integer NOT NULL,
    "SourceVersion" character varying(64) NOT NULL,
    "Status" text NOT NULL,
    "BrandingVersion" character varying(64) NOT NULL,
    "BrandingSourceRevision" bigint NOT NULL,
    "BrandingName" character varying(128) NOT NULL,
    "BrandingLogoUri" character varying(60000),
    "BrandingPrimaryColor" character varying(128) NOT NULL,
    "BrandingAccentColor" character varying(128) NOT NULL,
    "BrandingBackgroundColor" character varying(128) NOT NULL,
    "BrandingTextColor" character varying(128) NOT NULL,
    "BrandingHeadingFontFamily" character varying(128) NOT NULL,
    "BrandingBodyFontFamily" character varying(128) NOT NULL,
    "BrandingFooterText" character varying(128) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "RetentionExpiresAt" timestamp with time zone,
    "FinalizedAt" timestamp with time zone,
    "FinalizedArtifactName" character varying(128),
    "ReplacesDocumentId" uuid,
    "RequiresManualReview" boolean NOT NULL DEFAULT FALSE,
    "ManualReviewReasons" character varying(64) NOT NULL DEFAULT 'None',
    "VoidReason" character varying(64),
    "FinalizedArtifactContent" bytea,
    CONSTRAINT "PK_DocumentDrafts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DocumentDrafts_DocumentLineages_DocumentLineageId" FOREIGN KEY ("DocumentLineageId") REFERENCES "DocumentLineages" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Booking" (
    "Id" uuid NOT NULL,
    "TourId" uuid NOT NULL,
    "BasePrice" numeric NOT NULL,
    "RoomType" text NOT NULL,
    "RoomAdditionalCost" numeric NOT NULL,
    "PrincipalCustomer_CustomerId" uuid NOT NULL,
    "PrincipalCustomer_BikeType" text NOT NULL,
    "PrincipalCustomer_BikePrice" numeric NOT NULL,
    "CompanionCustomer_CustomerId" uuid,
    "CompanionCustomer_BikeType" text,
    "CompanionCustomer_BikePrice" numeric,
    "Discount_Type" text NOT NULL,
    "Discount_Amount" numeric NOT NULL,
    "Discount_Reason" character varying(2000),
    "BookingDate" timestamp with time zone NOT NULL,
    "Status" text NOT NULL,
    "Notes" character varying(2000),
    CONSTRAINT "PK_Booking" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Booking_Tours_TourId" FOREIGN KEY ("TourId") REFERENCES "Tours" ("Id") ON DELETE CASCADE
);

CREATE TABLE "DocumentDraftFields" (
    "FieldId" character varying(64) NOT NULL,
    "DocumentDraftId" uuid NOT NULL,
    "SortOrder" integer NOT NULL,
    "Label" character varying(128) NOT NULL,
    "Value" text NOT NULL,
    "PrivacyClassification" text NOT NULL,
    "IsEditable" boolean NOT NULL,
    "StaffOverride" character varying(4000),
    CONSTRAINT "PK_DocumentDraftFields" PRIMARY KEY ("DocumentDraftId", "FieldId"),
    CONSTRAINT "FK_DocumentDraftFields_DocumentDrafts_DocumentDraftId" FOREIGN KEY ("DocumentDraftId") REFERENCES "DocumentDrafts" ("Id") ON DELETE CASCADE
);

CREATE TABLE "BookingPaymentHistories" (
    "BookingId" uuid NOT NULL,
    "Version" bigint NOT NULL,
    "ProtectedState" bytea NOT NULL,
    CONSTRAINT "PK_BookingPaymentHistories" PRIMARY KEY ("BookingId"),
    CONSTRAINT "FK_BookingPaymentHistories_Booking_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Booking" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "PaymentInstructionSnapshots" (
    "DocumentId" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "AccountId" uuid NOT NULL,
    "AccountRevisionId" uuid NOT NULL,
    "Revision" integer NOT NULL,
    "ProtectedSnapshot" bytea NOT NULL,
    CONSTRAINT "PK_PaymentInstructionSnapshots" PRIMARY KEY ("DocumentId"),
    CONSTRAINT "FK_PaymentInstructionSnapshots_Booking_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Booking" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_PaymentInstructionSnapshots_DocumentDrafts_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "DocumentDrafts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PaymentInstructionSnapshots_PaymentAccounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "PaymentAccounts" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Booking_TourId" ON "Booking" ("TourId");

CREATE UNIQUE INDEX "UX_CustomerContactInfo_Email" ON "CustomerContactInfo" ("Email");

CREATE INDEX "IX_DocumentAuditRecords_DocumentLineageId" ON "DocumentAuditRecords" ("DocumentLineageId");

CREATE INDEX "IX_DocumentAuditRecords_RetentionExpiresAt" ON "DocumentAuditRecords" ("RetentionExpiresAt");

CREATE INDEX "IX_DocumentDrafts_RetentionExpiresAt_Unfinalized" ON "DocumentDrafts" ("RetentionExpiresAt") WHERE "FinalizedAt" IS NULL;

CREATE UNIQUE INDEX "UX_DocumentDrafts_BookingId_Type_Revision" ON "DocumentDrafts" ("BookingId", "Type", "Revision");

CREATE UNIQUE INDEX "UX_DocumentDrafts_DocumentLineageId_Revision" ON "DocumentDrafts" ("DocumentLineageId", "Revision");

CREATE UNIQUE INDEX "UX_DocumentLineages_BookingId_Type" ON "DocumentLineages" ("BookingId", "Type");

CREATE INDEX "IX_holds_Scope" ON disposition_hold.holds ("Scope");

CREATE INDEX "IX_idempotency_keys_RetentionOwner" ON messaging.idempotency_keys ("RetentionOwner");

CREATE INDEX "IX_inbox_messages_Outcome" ON branding_projection.inbox_messages ("Outcome");

CREATE UNIQUE INDEX "IX_outbox_messages_EventId" ON messaging.outbox_messages ("EventId");

CREATE INDEX "IX_outbox_messages_PublishedAt_EnqueuedAt" ON messaging.outbox_messages ("PublishedAt", "EnqueuedAt");

CREATE INDEX "IX_outbox_messages_PublishedAt_NextPublishAttemptAt_EnqueuedAt" ON messaging.outbox_messages ("PublishedAt", "NextPublishAttemptAt", "EnqueuedAt");

CREATE INDEX "IX_PaymentAudits_ResourceId_OccurredAt" ON "PaymentAudits" ("ResourceId", "OccurredAt");

CREATE UNIQUE INDEX "IX_PaymentAudits_Sequence" ON "PaymentAudits" ("Sequence");

CREATE INDEX "IX_PaymentInstructionSnapshots_AccountId" ON "PaymentInstructionSnapshots" ("AccountId");

CREATE INDEX "IX_PaymentInstructionSnapshots_BookingId" ON "PaymentInstructionSnapshots" ("BookingId");

CREATE INDEX "IX_recovery_audit_Kind_OldEpoch" ON branding_projection.recovery_audit ("Kind", "OldEpoch");

CREATE UNIQUE INDEX "IX_Tours_Identifier" ON "Tours" ("Identifier");

CREATE UNIQUE INDEX "IX_Tours_Name" ON "Tours" ("Name");

CREATE UNIQUE INDEX "IX_transport_messages_ConsumerName_EventId" ON messaging.transport_messages ("ConsumerName", "EventId");

CREATE INDEX "IX_transport_messages_ConsumerName_PoisonedAt" ON messaging.transport_messages ("ConsumerName", "PoisonedAt");

CREATE INDEX "IX_transport_messages_ConsumerName_ProcessedAt_NextConsumeAtte~" ON messaging.transport_messages ("ConsumerName", "ProcessedAt", "NextConsumeAttemptAt", "ReceivedAt");

ALTER TABLE "DocumentDrafts"
ADD COLUMN "ActiveFinalized" boolean
GENERATED ALWAYS AS (
    CASE WHEN "Status" = 'Finalized' THEN TRUE ELSE NULL END
) STORED;

ALTER TABLE "DocumentDrafts"
ADD CONSTRAINT "UQ_DocumentDrafts_ActiveFinalizedLineage"
UNIQUE ("DocumentLineageId", "ActiveFinalized")
DEFERRABLE INITIALLY DEFERRED;

CREATE FUNCTION public."EnforceDocumentDraftBookingEligibility"()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE booking_status text;
BEGIN
    SELECT "Status" INTO booking_status FROM public."Booking" WHERE "Id" = NEW."BookingId" FOR SHARE;
    IF NOT FOUND OR
        (NEW."Type" = 'PaymentInstructions' AND booking_status NOT IN ('Pending', 'Confirmed', 'Completed')) OR
        (NEW."Type" <> 'PaymentInstructions' AND booking_status NOT IN ('Confirmed', 'Completed')) THEN
        RAISE EXCEPTION USING ERRCODE = '23514', CONSTRAINT = 'CK_DocumentDrafts_BookingEligibility', MESSAGE = 'The document booking is ineligible.';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER "TR_DocumentDrafts_EnforceBookingEligibility"
BEFORE INSERT ON public."DocumentDrafts"
FOR EACH ROW EXECUTE FUNCTION public."EnforceDocumentDraftBookingEligibility"();

CREATE FUNCTION public."PreventDocumentAuditRecordMutation"()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'TRUNCATE' THEN
        RAISE EXCEPTION 'Document audit records cannot be truncated.';
    END IF;

    IF TG_OP = 'UPDATE' THEN
        RAISE EXCEPTION 'Document audit records are immutable.';
    END IF;

    IF OLD."RetentionExpiresAt" > CURRENT_TIMESTAMP THEN
        RAISE EXCEPTION 'Document audit records cannot be deleted before their retention period expires.';
    END IF;

    RETURN OLD;
END;
$$;

CREATE TRIGGER "TR_DocumentAuditRecords_RejectMutation"
BEFORE UPDATE OR DELETE ON "DocumentAuditRecords"
FOR EACH ROW EXECUTE FUNCTION public."PreventDocumentAuditRecordMutation"();

CREATE TRIGGER "TR_DocumentAuditRecords_RejectTruncate"
BEFORE TRUNCATE ON "DocumentAuditRecords"
FOR EACH STATEMENT EXECUTE FUNCTION public."PreventDocumentAuditRecordMutation"();

CREATE FUNCTION public."RejectPaymentHistoryMutation"()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'Payment records and audit evidence are append-only.';
END;
$$;
CREATE TRIGGER "TR_PaymentAudits_Immutable" BEFORE UPDATE OR DELETE ON "PaymentAudits"
    FOR EACH ROW EXECUTE FUNCTION public."RejectPaymentHistoryMutation"();
CREATE TRIGGER "TR_PaymentInstructions_Immutable" BEFORE UPDATE ON "PaymentInstructionSnapshots"
    FOR EACH ROW EXECUTE FUNCTION public."RejectPaymentHistoryMutation"();

CREATE FUNCTION public."EnforcePaymentVersionAdvance"()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NEW."Version" <= OLD."Version" THEN
        RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'Payment record versions must advance.';
    END IF;
    RETURN NEW;
END;
$$;
CREATE TRIGGER "TR_PaymentAccounts_Version" BEFORE UPDATE ON "PaymentAccounts"
    FOR EACH ROW EXECUTE FUNCTION public."EnforcePaymentVersionAdvance"();
CREATE TRIGGER "TR_BookingPaymentHistories_Version" BEFORE UPDATE ON "BookingPaymentHistories"
    FOR EACH ROW EXECUTE FUNCTION public."EnforcePaymentVersionAdvance"();

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261111091530_InitialAdmin', '10.0.15');

ALTER TABLE "DocumentDrafts" ADD "HasFinalizedArtifact" boolean NOT NULL DEFAULT FALSE;

CREATE TABLE "DocumentArtifactBodies" (
    "DocumentId" uuid NOT NULL,
    "Content" bytea NOT NULL,
    CONSTRAINT "PK_DocumentArtifactBodies" PRIMARY KEY ("DocumentId"),
    CONSTRAINT "FK_DocumentArtifactBodies_DocumentDrafts_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "DocumentDrafts" ("Id") ON DELETE CASCADE
);

INSERT INTO "DocumentArtifactBodies" ("DocumentId", "Content")
SELECT "Id", "FinalizedArtifactContent" FROM "DocumentDrafts"
WHERE "FinalizedArtifactContent" IS NOT NULL;

UPDATE "DocumentDrafts" SET "HasFinalizedArtifact" = TRUE
WHERE "FinalizedArtifactContent" IS NOT NULL;

ALTER TABLE "DocumentArtifactBodies"
ADD CONSTRAINT "CK_DocumentArtifactBodies_ContentLength"
CHECK (octet_length("Content") BETWEEN 1 AND 1048576) NOT VALID;

ALTER TABLE "DocumentDrafts" DROP COLUMN "FinalizedArtifactContent";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261118095052_SplitDocumentArtifactBodies', '10.0.15');

ALTER TABLE "Tours" ADD "CatalogRetirementAppliedAt" timestamp with time zone;

ALTER TABLE "Tours" ADD "RetiredAt" timestamp with time zone;

ALTER TABLE "Tours" ADD "RetirementId" uuid;

CREATE TABLE "TourRetirementAuditRecords" (
    "RetirementId" uuid NOT NULL,
    "TourId" uuid NOT NULL,
    "ActorId" character varying(128) NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "Outcome" text NOT NULL,
    CONSTRAINT "PK_TourRetirementAuditRecords" PRIMARY KEY ("RetirementId")
);

CREATE UNIQUE INDEX "IX_Tours_RetirementId" ON "Tours" ("RetirementId");

ALTER TABLE "Tours" ADD CONSTRAINT "CK_Tours_RetirementState" CHECK (("RetirementId" IS NULL) = ("RetiredAt" IS NULL)
AND ("CatalogRetirementAppliedAt" IS NULL OR "RetirementId" IS NOT NULL));

CREATE INDEX "IX_TourRetirementAuditRecords_TourId" ON "TourRetirementAuditRecords" ("TourId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119153856_AddTourRetirement', '10.0.15');

ALTER TABLE "DocumentDrafts" ADD "FinalizedArtifactLength" integer;

ALTER TABLE "DocumentDrafts" ADD "FinalizedArtifactSha256" character varying(64);

UPDATE "DocumentDrafts" AS revision
SET "FinalizedArtifactLength" = octet_length(body."Content"),
    "FinalizedArtifactSha256" = encode(sha256(body."Content"), 'hex')
FROM "DocumentArtifactBodies" AS body
WHERE revision."Id" = body."DocumentId";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119180233_RecordFinalizedArtifactIntegrity', '10.0.15');

ALTER TABLE "DocumentDrafts" ADD "FinalizedArtifactContentType" text;

ALTER TABLE "DocumentDrafts" ADD "FinalizedArtifactRendererVersion" text;

ALTER TABLE "DocumentDrafts" ADD "FinalizedArtifactRetentionPolicy" text;

UPDATE "DocumentDrafts"
SET "FinalizedArtifactRetentionPolicy" = 'DOC-RET-1.0'
WHERE "FinalizedAt" IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119182941_RecordFinalizedArtifactProvenance', '10.0.15');
