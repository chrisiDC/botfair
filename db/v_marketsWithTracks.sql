CREATE VIEW [dbo].[V_MarketsWithTracks]
as

select distinct(m.id),m.name,m.eventdate,m.isHot from markets m
inner join  pricetrack p on p.fk_market=m.id
