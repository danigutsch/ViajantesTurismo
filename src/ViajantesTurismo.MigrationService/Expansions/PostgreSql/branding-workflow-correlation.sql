ALTER TABLE branding.outbox_messages ADD "WorkflowCorrelationId" uuid;

CREATE INDEX "IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id" ON branding.outbox_messages ("WorkflowCorrelationId", "EnqueuedAt" DESC, "Id" DESC);

INSERT INTO public."__EFMigrationsHistory_Branding" ("MigrationId", "ProductVersion")
VALUES ('20261126125357_AddBrandingWorkflowCorrelation', '10.0.15');
