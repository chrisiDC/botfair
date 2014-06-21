USE [botfair]
GO
/****** Object:  StoredProcedure [dbo].[GetDataCache]    Script Date: 11/13/2012 09:28:53 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[GetDataCache]
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

