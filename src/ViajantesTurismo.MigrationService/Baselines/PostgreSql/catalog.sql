CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'messaging') THEN
        CREATE SCHEMA messaging;
    END IF;
END $EF$;

CREATE TABLE "CatalogTourReadModels" (
    "CatalogTourId" uuid NOT NULL,
    "AdminTourId" uuid NOT NULL,
    "Identifier" character varying(64) NOT NULL,
    "Title" character varying(128) NOT NULL,
    "Slug" character varying(128) NOT NULL,
    "Summary" character varying(4000) NOT NULL,
    "Description" character varying(4000) NOT NULL,
    "Itinerary" character varying(4000) NOT NULL,
    "SeoTitle" character varying(128) NOT NULL,
    "SeoDescription" character varying(4000) NOT NULL,
    "IsPublished" boolean NOT NULL,
    "StreamVersion" bigint NOT NULL DEFAULT 1,
    "PresentationPosition" bigint NOT NULL,
    "PublicationPosition" bigint NOT NULL,
    "Position" bigint NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CatalogTourReadModels" PRIMARY KEY ("CatalogTourId")
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

CREATE TABLE "PublicContent" (
    "Id" uuid NOT NULL,
    "Key" character varying(64) NOT NULL,
    "SourceLanguage" character varying(35) NOT NULL,
    "PublicationState" text NOT NULL,
    CONSTRAINT "PK_PublicContent" PRIMARY KEY ("Id")
);

CREATE TABLE "PublicMediaImages" (
    "Id" uuid NOT NULL,
    "SourceObjectKey" character varying(1024) NOT NULL,
    "Checksum" character varying(128) NOT NULL,
    "ContentType" character varying(128) NOT NULL,
    "FileSizeBytes" bigint NOT NULL,
    "Width" integer NOT NULL,
    "Height" integer NOT NULL,
    "ProcessingStatus" text NOT NULL,
    "AltText" character varying(256) NOT NULL,
    "Caption" character varying(256),
    "IsDecorative" boolean NOT NULL,
    "RequiresHumanReview" boolean NOT NULL,
    "IsAiGenerated" boolean NOT NULL,
    "Attribution" character varying(256),
    "Copyright" character varying(256),
    "Tags" text[] NOT NULL,
    CONSTRAINT "PK_PublicMediaImages" PRIMARY KEY ("Id")
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

CREATE TABLE "PublicContentVariants" (
    "Language" character varying(35) NOT NULL,
    "PublicContentId" uuid NOT NULL,
    "Title" character varying(128) NOT NULL,
    "Body" character varying(4000) NOT NULL,
    "SeoTitle" character varying(128),
    "MetaDescription" character varying(256),
    "ShareSummary" character varying(256),
    "RequiresHumanReview" boolean NOT NULL,
    CONSTRAINT "PK_PublicContentVariants" PRIMARY KEY ("PublicContentId", "Language"),
    CONSTRAINT "FK_PublicContentVariants_PublicContent_PublicContentId" FOREIGN KEY ("PublicContentId") REFERENCES "PublicContent" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PublicMediaImageAccessibilityTexts" (
    "Language" character varying(35) NOT NULL,
    "PublicMediaImageId" uuid NOT NULL,
    "AltText" character varying(256),
    "Caption" character varying(256),
    "IsDecorative" boolean NOT NULL,
    "RequiresHumanReview" boolean NOT NULL,
    "IsAiGenerated" boolean NOT NULL,
    "PendingDraftCreatedAt" timestamp with time zone,
    CONSTRAINT "PK_PublicMediaImageAccessibilityTexts" PRIMARY KEY ("PublicMediaImageId", "Language"),
    CONSTRAINT "FK_PublicMediaImageAccessibilityTexts_PublicMediaImages_Public~" FOREIGN KEY ("PublicMediaImageId") REFERENCES "PublicMediaImages" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PublicMediaImageResponsiveVariants" (
    "SortOrder" integer NOT NULL,
    "PublicMediaImageId" uuid NOT NULL,
    "ObjectKey" character varying(1024) NOT NULL,
    "Width" integer NOT NULL,
    "Height" integer NOT NULL,
    "ContentType" character varying(128) NOT NULL,
    "FileSizeBytes" bigint NOT NULL,
    CONSTRAINT "PK_PublicMediaImageResponsiveVariants" PRIMARY KEY ("PublicMediaImageId", "SortOrder"),
    CONSTRAINT "FK_PublicMediaImageResponsiveVariants_PublicMediaImages_Public~" FOREIGN KEY ("PublicMediaImageId") REFERENCES "PublicMediaImages" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PublicMediaImageTourLinks" (
    "CatalogTourId" uuid NOT NULL,
    "PublicMediaImageId" uuid NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "IsCover" boolean NOT NULL,
    CONSTRAINT "PK_PublicMediaImageTourLinks" PRIMARY KEY ("PublicMediaImageId", "CatalogTourId"),
    CONSTRAINT "FK_PublicMediaImageTourLinks_PublicMediaImages_PublicMediaImag~" FOREIGN KEY ("PublicMediaImageId") REFERENCES "PublicMediaImages" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_CatalogTourReadModels_AdminTourId" ON "CatalogTourReadModels" ("AdminTourId");

CREATE UNIQUE INDEX "IX_CatalogTourReadModels_Slug" ON "CatalogTourReadModels" ("Slug");

CREATE INDEX "IX_idempotency_keys_RetentionOwner" ON messaging.idempotency_keys ("RetentionOwner");

CREATE UNIQUE INDEX "IX_outbox_messages_EventId" ON messaging.outbox_messages ("EventId");

CREATE INDEX "IX_outbox_messages_PublishedAt_EnqueuedAt" ON messaging.outbox_messages ("PublishedAt", "EnqueuedAt");

CREATE INDEX "IX_outbox_messages_PublishedAt_NextPublishAttemptAt_EnqueuedAt" ON messaging.outbox_messages ("PublishedAt", "NextPublishAttemptAt", "EnqueuedAt");

CREATE UNIQUE INDEX "IX_PublicContent_Key" ON "PublicContent" ("Key");

CREATE UNIQUE INDEX "UX_PublicMediaImageTourLinks_CatalogTourId_Cover" ON "PublicMediaImageTourLinks" ("CatalogTourId") WHERE "IsCover" = TRUE;

CREATE UNIQUE INDEX "UX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder" ON "PublicMediaImageTourLinks" ("CatalogTourId", "DisplayOrder");

CREATE UNIQUE INDEX "IX_transport_messages_ConsumerName_EventId" ON messaging.transport_messages ("ConsumerName", "EventId");

CREATE INDEX "IX_transport_messages_ConsumerName_PoisonedAt" ON messaging.transport_messages ("ConsumerName", "PoisonedAt");

CREATE INDEX "IX_transport_messages_ConsumerName_ProcessedAt_NextConsumeAtte~" ON messaging.transport_messages ("ConsumerName", "ProcessedAt", "NextConsumeAttemptAt", "ReceivedAt");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261111091535_InitialCatalog', '10.0.15');

ALTER TABLE messaging.transport_messages ADD CONSTRAINT "CK_transport_messages_branding_payload_size" CHECK ("ConsumerName" <> 'admin'
OR "EventType" <> 'branding.settings.changed'
OR octet_length("Payload") <= 65536);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261118110059_LimitBrandingTransportPayload', '10.0.15');

CREATE TABLE "CatalogTourRetirements" (
    "AdminTourId" uuid NOT NULL,
    "RetirementId" uuid NOT NULL,
    "WithdrawnAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CatalogTourRetirements" PRIMARY KEY ("AdminTourId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119161057_RetainTourRetirements', '10.0.15');

ALTER TABLE "PublicMediaImages" ADD "UploadFingerprint" character varying(64);

CREATE UNIQUE INDEX "IX_PublicMediaImages_UploadFingerprint" ON "PublicMediaImages" ("UploadFingerprint");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119170602_AddCatalogMediaUploadFingerprint', '10.0.15');
