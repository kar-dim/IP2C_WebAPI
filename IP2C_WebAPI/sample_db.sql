/****** Object: Table [dbo].[Countries] Script Date: 12/10/2022 12:07:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Countries](
[Id] [int] IDENTITY(1,1) NOT NULL,
[Name] [varchar](50) NOT NULL,
[TwoLetterCode] [char](2) NOT NULL,
[ThreeLetterCode] [char](3) NOT NULL,
[CreatedAt] [datetime2](7) NOT NULL,
CONSTRAINT [PK_Countries] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 95,
OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object: Table [dbo].[IPAddresses] Script Date: 12/10/2022 12:07:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[IPAddresses](
[Id] [int] IDENTITY(1,1) NOT NULL,
[CountryId] [int] NOT NULL,
[IP] [varchar](15) NOT NULL,
[CreatedAt] [datetime2](7) NOT NULL,
[UpdatedAt] [datetime2](7) NOT NULL,
CONSTRAINT [PK_IPAddresses] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 95,
OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[Countries] ON
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (1,
N'Greece', N'GR', N'GRC', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (2,
N'Germany', N'DE', N'DEU', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (3,
N'Cyprus', N'CY', N'CYP', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (4,
N'United States', N'US', N'USA', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (6,
N'Spain', N'ES', N'ESP', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (7,
N'France', N'FR', N'FRA', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (8,
N'Italy', N'IT', N'IA ', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (9,
N'Japan', N'JP', N'JPN', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
INSERT [dbo].[Countries] ([Id], [Name], [TwoLetterCode], [ThreeLetterCode], [CreatedAt]) VALUES (10,
N'China', N'CN', N'CHN', CAST(N'2022-10-12T06:46:10.5000000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[Countries] OFF
GO
SET IDENTITY_INSERT [dbo].[IPAddresses] ON
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (6, 1,
N'44.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (7, 2,
N'45.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (8, 3,
N'46.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (9, 4,
N'47.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (10, 6,
N'49.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (11, 7,
N'41.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (12, 8,
N'42.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (13, 9,
N'43.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (14, 10,
N'50.255.255.254', CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2),
CAST(N'2022-10-12T07:04:06.8566667' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (15, 1,
N'44.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (16, 2,
N'45.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (17, 3,
N'46.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (18, 4,
N'47.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (19, 6,
N'49.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (20, 7,
N'41.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (21, 8,
N'42.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (22, 9,
N'43.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (23, 10,
N'50.25.55.254', CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2),
CAST(N'2022-10-12T07:04:33.3800000' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (24, 1,
N'44.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (25, 2,
N'45.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (26, 3,
N'46.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (27, 4,
N'47.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (28, 6,
N'49.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (29, 7,
N'41.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (30, 8,
N'42.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (31, 9,
N'43.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (32, 10,
N'50.25.55.4', CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2),
CAST(N'2022-10-12T07:04:51.3233333' AS DateTime2))
GO
INSERT [dbo].[IPAddresses] ([Id], [CountryId], [IP], [CreatedAt], [UpdatedAt]) VALUES (33, 1,
N'10.20.30.40', CAST(N'2022-10-12T08:41:37.3100000' AS DateTime2),
CAST(N'2022-10-12T08:41:37.3100000' AS DateTime2))
GO
SET IDENTITY_INSERT [dbo].[IPAddresses] OFF
GO
SET ANSI_PADDING ON
GO
/****** Object: Index [IX_IPAddresses] Script Date: 12/10/2022 12:07:23 ******/
ALTER TABLE [dbo].[IPAddresses] ADD CONSTRAINT [IX_IPAddresses] UNIQUE NONCLUSTERED
(
[IP] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF,
IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON,
FILLFACTOR = 95, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Countries] ADD CONSTRAINT [DF_Countries_CreatedAt] DEFAULT (getutcdate())
FOR [CreatedAt]
GO
ALTER TABLE [dbo].[IPAddresses] ADD CONSTRAINT [DF_IPAddresses_CreatedAt] DEFAULT
(getutcdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[IPAddresses] ADD CONSTRAINT [DF_IPAddresses_UpdatedAt] DEFAULT
(getutcdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[IPAddresses] WITH CHECK ADD CONSTRAINT [FK_IPAddresses_Countries]
FOREIGN KEY([CountryId])
REFERENCES [dbo].[Countries] ([Id])
GO
ALTER TABLE [dbo].[IPAddresses] CHECK CONSTRAINT [FK_IPAddresses_Countries]
GO