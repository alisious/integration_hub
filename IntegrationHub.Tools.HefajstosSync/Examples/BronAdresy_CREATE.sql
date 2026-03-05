USE [IntegrationHubDB]
GO

/****** Object:  Table [piesp].[BronAdresy]    Script Date: 5.03.2026 10:42:25 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [piesp].[BronAdresy](
	[BA_BOPESEL] [varchar](11) NOT NULL,
	[BA_MIEJSCOWOSC] [varchar](100) NOT NULL,
	[BA_ULICA] [varchar](100) NOT NULL,
	[BA_NUMER_DOMU] [varchar](10) NOT NULL,
	[BA_NUMER_LOKALU] [varchar](10) NULL,
	[BA_KOD_POCZTOWY] [varchar](10) NULL,
	[BA_POCZTA] [varchar](100) NOT NULL,
	[BA_OPIS] [varchar](1000) NULL,
	[BA_DATA_AKTUALIZACJI] [datetime] NOT NULL
) ON [PRIMARY]
GO

ALTER TABLE [piesp].[BronAdresy] ADD  CONSTRAINT [DF_BronAdresy_BA_DATA_AKTUALIZACJI]  DEFAULT (getdate()) FOR [BA_DATA_AKTUALIZACJI]
GO

