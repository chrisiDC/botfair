

CREATE TABLE [dbo].[PriceTrack](
	[fk_market] [int] NOT NULL,
	[fk_selection] [int] NOT NULL,
	[priceDate] [datetime] NOT NULL,
	[isLay] [bit] NOT NULL,
	[price] [float] NOT NULL,
 CONSTRAINT [PK_PriceTrack] PRIMARY KEY CLUSTERED 
(
	[fk_market] ASC,
	[fk_selection] ASC,
	[priceDate] ASC,
	[isLay] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]


