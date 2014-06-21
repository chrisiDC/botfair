


GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bets]') AND type in (N'U'))
DROP TABLE [dbo].[Bets]
GO

GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BetMasters]') AND type in (N'U'))
DROP TABLE [dbo].BetMasters
GO

GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[configuration]') AND type in (N'U'))
DROP TABLE [dbo].configuration
GO

GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[log]') AND type in (N'U'))
DROP TABLE [dbo].[log]
GO

GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[eventtypes]') AND type in (N'U'))
DROP TABLE [dbo].eventtypes
GO

GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[markets]') AND type in (N'U'))
DROP TABLE [dbo].markets
GO

GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[pricetrack]') AND type in (N'U'))
DROP TABLE [dbo].pricetrack
GO


/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[V_hotmarkets]'))
DROP VIEW [dbo].V_hotmarkets
GO


/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[selections]') AND type in (N'U'))
DROP TABLE [dbo].selections
GO


/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[V_activebets]') )
DROP View [dbo].V_activebets
GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[[V_marketswithtracks]]') )
DROP View [dbo].[V_marketswithtracks]
GO

/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[[V_selectionswithtracks]]') )
DROP View [dbo].[V_marketswithtracks]
GO




/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[V_bets]'))
DROP View [dbo].V_bets
GO




/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[V_markets]'))
DROP View [dbo].V_markets
GO


/****** Object:  Table [dbo].[Bets]    Script Date: 10/18/2012 17:39:14 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[V_pricetrack]'))
DROP View [dbo].V_pricetrack
GO

/****** Object:  StoredProcedure [dbo].[GetDataCache]    Script Date: 10/18/2012 17:41:34 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetDataCache]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[GetDataCache]
GO

GO
CREATE PROCEDURE [dbo].[GetDataCache]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	declare @now as datetime;
	set @now = GETDATE();
	
	update selections set tracked=0;
    update markets set ishot=0;
    update markets set [marketstatus]='closed' where eventDate < @now;
     update markets set [marketstatus]='active' where eventDate >= @now;
 
     delete from selections where marketId in (select id from markets where eventDate >= @now);
     delete from pricetrack where fk_market in (select id from markets where eventDate >= @now);
             
    select * from markets  where eventDate >= @now;
		--datepart(year,eventDate) = datepart(year,@now) and
		--datepart(month,eventDate) = datepart(month,@now) and
		--datepart(day,eventDate) = datepart(day,@now) 
		
		select * from bets where fk_market in (select id from markets  where eventDate >= @now) 
	--select * from bets inner join Markets on fk_market = Markets.id
	--where Markets.eventDate >= @now
	
	select * from selections where marketId in (select fk_market from bets where fk_market in (select id from markets  where eventDate >= @now))
	
	
	select * from pricetrack where fk_market in (select fk_market from bets where fk_market in (select id from markets  where eventDate >= @now ))
	
	
	select * from Configuration
END
GO

CREATE TABLE [dbo].[BetMasters](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[description] [nvarchar](250) NULL,
	[class] [nvarchar] (250) NOT NULL,
	[active] [bit]
 CONSTRAINT [PK_BetMasters] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
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

CREATE TABLE [dbo].[EventTypes](
	[id] [int] NOT NULL,
	[name] [nvarchar](100) NULL,
 CONSTRAINT [PK_EventTypes] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]



CREATE TABLE [dbo].[Log](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[type] [nvarchar](255) NOT NULL,
	[message] [ntext] NULL,
	[eventId] [int] NULL,
	[date] [datetime] NOT NULL,
 CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

CREATE TABLE [dbo].[Markets](
	[id] [int] NOT NULL,
	[name] [nvarchar](100) NULL,
	[menuPath] [nvarchar](500) NULL,
	[totalAmount] [float] NULL,
	[eventDate] [datetime] NULL,
	[type] [nvarchar](100) NULL,
	[eventType] [int] NOT NULL,
	[eventHierarchy] [nvarchar](100) NULL,
	[betDelay] [nvarchar](100) NULL,
	[runners] [int] NULL,
	[winners] [int] NULL,
	[bspMarket] [bit] NULL,
	[turningInPlay] [bit] NULL,
	[isHot] [bit] NOT NULL,
	[wasHot] [bit] NOT NULL,
	[interval] [float] NOT NULL,
	[country] [nvarchar](100) NULL,
	[marketStatus] [nvarchar](50) NOT NULL,
	[timeScanned] [datetime] NOT NULL,
	[marketSuspendTime] [datetime] NULL,
 CONSTRAINT [PK__Markets__00000000000000A5] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[Markets] ADD  CONSTRAINT [DF_Markets_wasHot]  DEFAULT ((0)) FOR [wasHot]
GO



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



GO


CREATE VIEW [dbo].[V_ActiveBets]
as

select m.id as marketId,m.menuPath,m.marketStatus,m.eventDate, b.* from [bets] b
inner join Markets m on b.fk_market = m.id
where b.sizePosted > 0 and m.marketStatus = 'active'
GO

CREATE VIEW [dbo].[V_MarketsWithTracks]
as

select distinct(m.id),m.name,m.eventdate,m.isHot from markets m
inner join  pricetrack p on p.fk_market=m.id


GO

CREATE VIEW [dbo].[V_Bets]
as

select m.id as marketId,m.name,m.eventDate,s.name as selectionName, b.* from [bets] b
inner join Markets m on b.fk_market = m.id
inner join Selections s on s.selectionId = b.fk_selection
where b.datePosted > '1970-01-01 00:00:00.000'

GO



CREATE VIEW [dbo].[V_HotMarkets]
AS
select id,name,eventDate as [date],country,marketStatus from Markets where isHot=1


GO
CREATE VIEW [dbo].[V_Markets]
as
select id,name,menuPath,totalAmount,eventDate as [date],runners,winners,country,marketStatus from [markets]

GO

CREATE VIEW [dbo].[V_PriceTrack]
AS
SELECT DISTINCT p.fk_selection AS selectionId, p.fk_market AS marketId, s.name AS horse, m.name AS marketName,p.priceDate,p.IsLay,p.Price
FROM         dbo.PriceTrack AS p INNER JOIN
                      dbo.Selections AS s ON s.selectionId = p.fk_selection AND s.marketId = p.fk_market INNER JOIN
                      dbo.Markets AS m ON m.id = p.fk_market

where m.ishot = 1

GO


CREATE VIEW [dbo].[V_SelectionsWithTracks]
as

select distinct(s.selectionId),s.name,p.fk_market marketId from selections s
inner join  pricetrack p on p.fk_selection=s.selectionId

GO

insert into configuration values(300,5,100,6000)

GO

insert into betmasters values('test','BotFair.BusinessLayer.BetEngine.Masters.MyFirstMaster',1)

Go












