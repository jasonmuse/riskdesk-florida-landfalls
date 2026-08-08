using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Union;


namespace RiskDesk.Api.Services;

public class FloridaBoundaryLoader
{
    public Geometry Load(string filePath)
    {
        // Read the Florida boundary from the GeoJSON file.
        var geoJson = File.ReadAllText(filePath);
        var reader = new GeoJsonReader();

        var featureCollection = reader.Read<FeatureCollection>(geoJson);

        // The file contains one Florida feature, so use its geometry.
        var firstFeature = featureCollection.First();
        var geometry = firstFeature.Geometry;

        return geometry;

    }

    public Geometry LoadCombined(string filePath)
    {
        var geoJson = File.ReadAllText(filePath);
        var reader = new GeoJsonReader();
        var featureCollection = reader.Read<FeatureCollection>(geoJson);

        return UnaryUnionOp.Union(
            featureCollection.Select(feature => feature.Geometry));
    }
    
}
