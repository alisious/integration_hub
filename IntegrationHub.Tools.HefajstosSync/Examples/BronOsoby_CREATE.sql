USE [IntegrationHubDB]
GO

/****** Object:  Table [piesp].[BronOsoby]    Script Date: 5.03.2026 10:42:49 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [piesp].[BronOsoby](
	[BO_PESEL] [varchar](11) NOT NULL,
        [BA_OPIS] [varchar](MAX) NULL,
	[BO_DATA_AKTUALIZACJI] [datetime] NULL
) ON [PRIMARY]
GO

ALTER TABLE [piesp].[BronOsoby] ADD  CONSTRAINT [DF_BronOsoby_BO_DATA_AKTUALIZACJI]  DEFAULT (getdate()) FOR [BO_DATA_AKTUALIZACJI]
GO

