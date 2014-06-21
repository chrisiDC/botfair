



CREATE VIEW [dbo].[V_PriceTrack]
AS
SELECT DISTINCT p.fk_selection AS selectionId, p.fk_market AS marketId, s.name AS horse, m.name AS marketName,p.priceDate,p.IsLay,p.Price
FROM         dbo.PriceTrack AS p INNER JOIN
                      dbo.Selections AS s ON s.selectionId = p.fk_selection AND s.marketId = p.fk_market INNER JOIN
                      dbo.Markets AS m ON m.id = p.fk_market

where m.ishot = 1

