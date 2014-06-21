CREATE TABLE [dbo].[Selections](
	[marketId] [int] NOT NULL,
	[selectionId] [int] NOT NULL,
	[tracked] [bit] NOT NULL,
	[position] [int] NOT NULL,
	[name] [varchar](50) NULL,
	[asianLineId] [int] NULL,
	[handicap] [float] NULL,
 CONSTRAINT [PK__Selections__0000000000000169] PRIMARY KEY CLUSTERED 
(
	[marketId] ASC,
	[selectionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]