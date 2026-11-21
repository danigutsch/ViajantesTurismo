CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory_Branding" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory_Branding" PRIMARY KEY ("MigrationId")
);

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'branding') THEN
        CREATE SCHEMA branding;
    END IF;
END $EF$;

CREATE TABLE branding."BrandingLogos" (
    "Sha256" character(64) NOT NULL,
    "ContentType" character varying(32) NOT NULL,
    "Content" bytea NOT NULL,
    "StoredAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_BrandingLogos" PRIMARY KEY ("Sha256")
);

CREATE TABLE branding."BrandingRecoveryAudits" (
    "Id" uuid NOT NULL,
    "Kind" character varying(32) NOT NULL,
    "OldEpoch" uuid NOT NULL,
    "OldRevision" bigint NOT NULL,
    "NewEpoch" uuid NOT NULL,
    "NewRevision" bigint NOT NULL,
    "EventId" uuid NOT NULL,
    "Reason" character varying(256) NOT NULL,
    "IncidentReference" character varying(256) NOT NULL,
    "Actor" character varying(256) NOT NULL,
    "RecordedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_BrandingRecoveryAudits" PRIMARY KEY ("Id")
);

CREATE TABLE branding.outbox_messages (
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

CREATE TABLE branding."BrandingSettings" (
    "Id" uuid NOT NULL,
    "BrandName" character varying(120) NOT NULL,
    "PrimaryColor" character varying(7) NOT NULL,
    "AccentColor" character varying(7) NOT NULL,
    "BackgroundColor" character varying(7) NOT NULL,
    "TextColor" character varying(7) NOT NULL,
    "HeadingFontFamily" character varying(120) NOT NULL,
    "BodyFontFamily" character varying(120) NOT NULL,
    "LogoUri" character varying(2048),
    "LogoSha256" character(64),
    "SourceEpoch" uuid NOT NULL,
    "SourceRevision" bigint NOT NULL,
    CONSTRAINT "PK_BrandingSettings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BrandingSettings_BrandingLogos_LogoSha256" FOREIGN KEY ("LogoSha256") REFERENCES branding."BrandingLogos" ("Sha256") ON DELETE RESTRICT
);

CREATE TABLE branding."BrandingSettingsRevisions" (
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
    "LogoSha256" character(64),
    "CommittedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_BrandingSettingsRevisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BrandingSettingsRevisions_BrandingLogos_LogoSha256" FOREIGN KEY ("LogoSha256") REFERENCES branding."BrandingLogos" ("Sha256") ON DELETE RESTRICT
);

CREATE INDEX "IX_BrandingSettings_LogoSha256" ON branding."BrandingSettings" ("LogoSha256");

CREATE INDEX "IX_BrandingSettingsRevisions_LogoSha256" ON branding."BrandingSettingsRevisions" ("LogoSha256");

CREATE UNIQUE INDEX "IX_BrandingSettingsRevisions_SourceEpoch_SourceRevision" ON branding."BrandingSettingsRevisions" ("SourceEpoch", "SourceRevision");

CREATE UNIQUE INDEX "IX_outbox_messages_EventId" ON branding.outbox_messages ("EventId");

CREATE INDEX "IX_outbox_messages_PublishedAt_EnqueuedAt" ON branding.outbox_messages ("PublishedAt", "EnqueuedAt");

CREATE INDEX "IX_outbox_messages_PublishedAt_NextPublishAttemptAt_EnqueuedAt" ON branding.outbox_messages ("PublishedAt", "NextPublishAttemptAt", "EnqueuedAt");

INSERT INTO public."__EFMigrationsHistory_Branding" ("MigrationId", "ProductVersion")
VALUES ('20261111091540_InitialBranding', '10.0.15');

ALTER TABLE branding."BrandingLogos" ADD CONSTRAINT "CK_BrandingLogos_ContentLength" CHECK (octet_length("Content") BETWEEN 1 AND 40960);

ALTER TABLE branding.outbox_messages ADD CONSTRAINT "CK_BrandingOutbox_PayloadLength" CHECK (octet_length("Payload") <= 65536);

INSERT INTO public."__EFMigrationsHistory_Branding" ("MigrationId", "ProductVersion")
VALUES ('20261118105700_BoundBrandingLogoStorage', '10.0.15');
