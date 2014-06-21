

CREATE VIEW [dbo].[V_HotMarkets]
AS
select id,name,eventDate as [date],country,marketStatus from Markets where isHot=1



