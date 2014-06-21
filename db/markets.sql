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