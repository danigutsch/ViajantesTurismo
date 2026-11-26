ALTER TABLE messaging.transport_messages ADD "WorkflowCorrelationId" uuid;

ALTER TABLE messaging.outbox_messages ADD "WorkflowCorrelationId" uuid;

CREATE INDEX "IX_transport_messages_ConsumerName_WorkflowCorrelationId_Recei~" ON messaging.transport_messages ("ConsumerName", "WorkflowCorrelationId", "ReceivedAt" DESC, "Id" DESC);

CREATE INDEX "IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id" ON messaging.outbox_messages ("WorkflowCorrelationId", "EnqueuedAt" DESC, "Id" DESC);

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261126125355_AddCatalogWorkflowCorrelation', '10.0.15');
