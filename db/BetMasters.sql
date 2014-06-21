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