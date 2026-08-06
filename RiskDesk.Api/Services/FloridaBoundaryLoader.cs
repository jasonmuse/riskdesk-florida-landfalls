using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;


namespace RiskDesk.Api.Services;

public class FloridaBoundaryLoader
{
    public Geometry Load(string filePath)
    {

        var geoJson = File.ReadAllText(filePath);
        var reader = new GeoJsonReader();

        var featureCollection = reader.Read<FeatureCollection>(geoJson);

        var firstFeature = featureCollection.First();
        var geometry = firstFeature.Geometry;

        return geometry;

    }
    
}