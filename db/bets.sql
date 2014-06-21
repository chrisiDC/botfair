
CREATE TABLE [dbo].[Bets](
	[fk_market] [int] NOT NULL,
	[fk_selection] [int] NOT NULL,
	[amount] [float] NULL,
	[isLay] [bit] NOT NULL,
	[firstPrice] [float] NULL,
	[currentPrice] [float] NULL,
	[pricePosted] [float] NULL,
	[sizePosted] [float] NULL,
	[datePosted] [datetime] NULL,
	[errorCode] [nvarchar](255) NULL,
	[betFairId] [bigint] NULL,
	[masterId] [int] NOT NULL,
 CONSTRAINT [PK__Bets__0000000000000220] PRIMARY KEY CLUSTERED 
(
	[fk_selection] ASC,
	[fk_market] ASC,
	[isLay] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]


