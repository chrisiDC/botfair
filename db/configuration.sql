

CREATE TABLE [dbo].[Configuration](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[hotMarketsSeconds] [int] NOT NULL,
	[percentage] [float] NOT NULL,
	[riskValue] [int] NOT NULL,
	[newMarketsPeriod] [int] NOT NULL,
 CONSTRAINT [PK_BotState3] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]



