CREATE TABLE [dbo].[RadioFrequencies] (
    [Id]            BIGINT       IDENTITY (1, 1) NOT NULL,
    [RadioSystemId] BIGINT       NOT NULL,
    [FrequencyHz]   DECIMAL (20) NOT NULL,
    [ControlData]   BIT          NOT NULL,
    CONSTRAINT [PK_RadioFrequencies] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RadioFrequencies_RadioSystems_RadioSystemId] FOREIGN KEY ([RadioSystemId]) REFERENCES [dbo].[RadioSystems] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_RadioFrequencies_RadioSystemId]
    ON [dbo].[RadioFrequencies]([RadioSystemId] ASC);

