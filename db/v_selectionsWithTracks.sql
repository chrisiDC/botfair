

CREATE VIEW [dbo].[V_SelectionsWithTracks]
as

select distinct(s.selectionId),s.name,p.fk_market marketId from selections s
inner join  pricetrack p on p.fk_selection=s.selectionId
