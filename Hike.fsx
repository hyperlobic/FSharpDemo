//#load "Scripts/load-references-debug.fsx"
#r "packages/FSharp.Data.2.2.5/lib/net40/FSharp.Data.dll"
#r "packages/SharpKml.Core.2.1.3/lib/portable-net4+sl5+wp8+win8/SharpKml.dll"
#r "packages/FSharp.Charting.0.90.13/lib/net40/FSharp.Charting.dll"
#load "packages/FsLab.0.3.19/FsLab.fsx"

open System
open System.Globalization
open FSharp.Data
open SharpKml.Base
open SharpKml.Dom


[<Literal>]
let baruHikeCsv = __SOURCE_DIRECTORY__ +  @"\09010800.csv"

type VisiontacTrack = CsvProvider<baruHikeCsv, Schema="DATE=string, TIME=string">

let trackCsv = VisiontacTrack.Load(baruHikeCsv)

trackCsv.Headers

let printRow (row: VisiontacTrack.Row) = 
    printfn "Date: %s Time: %s LatLong: (%s, %s) Height: %i Speed: %i" row.DATE row.TIME row.``LATITUDE N/S`` row.``LONGITUDE E/W`` row.HEIGHT row.SPEED

trackCsv.Rows
|> Seq.take 20
|> Seq.iter printRow

trackCsv.Rows
|> Seq.filter (fun r -> r.SPEED > 50)
|> Seq.iter printRow

// Define a record of position data with our conversions applied
type Position = {
    Time: DateTimeOffset;
    LatLong: float * float;
    Height: float;
    Speed: float;
    Heading: int;
}

// Create some functions to convert the raw values from the CSV to more useful data

// Parse the raw strings to create a DateTimeOffset 
let parseDateTime (row: VisiontacTrack.Row) =
    DateTimeOffset.ParseExact(row.DATE + row.TIME, "yyMMddHHmmss", null, DateTimeStyles.AssumeUniversal)

// Convert the time to Eastern time
let easternTime (time: DateTimeOffset) =
    time.ToOffset(new System.TimeSpan(0, -5, 0, 0))

// Use the composition operator ">>"" to create a new function that performs both the string parsing and EST conversion
let parseEasternTime = parseDateTime >> easternTime

let metersToFeet meters = meters * 3.28084

// Parse the lat or long string and convert to a number with the correct sign
let parseLatOrLongString (latOrLongString: string) = 
    let direction = latOrLongString.[latOrLongString.Length - 1]
    let value = latOrLongString.Substring(0, latOrLongString.Length - 1).AsFloat()
    
    if direction = 'N' || direction = 'E' 
    then value 
    else -value

// From the lat long string values, return the lat long as a pair of numbers (a tuple)
let parseLatLong (row: VisiontacTrack.Row) =
    (parseLatOrLongString row.``LATITUDE N/S``), (parseLatOrLongString row.``LONGITUDE E/W``) 

let heightToFeet (row: VisiontacTrack.Row) =
    row.HEIGHT
    |> float
    |> metersToFeet

// Convert an entire row to our Point record
let convertRow (row: VisiontacTrack.Row) = 
    {
        Time = parseEasternTime row;
        LatLong = parseLatLong row;
        Height = heightToFeet row;
        Speed = float row.SPEED;
        Heading = row.HEADING;
    }

// Convert all the rows to an array of Points

let positions = 
    trackCsv.Rows
    |> Seq.map convertRow
    |> Seq.toArray


////////////////////
/// Find the max altitude

let maxAltitudeAndTime =
    positions
    |> Array.maxBy (fun x -> x.Height)

printfn "Max height = %f at %s" maxAltitudeAndTime.Height (maxAltitudeAndTime.Time.ToString())


///////////////////
/// Show an elevation chart

open FSharp.Charting

// Create a list of tuples containing the DateTime and Height
let altitudes =
    [ for row in positions do
        yield row.Time, row.Height ]

Chart.FastLine(altitudes, "Elevation Chart")


///////////////////
/// Show a speed chart

let speed = 
    positions
    |> Array.map (fun x -> x.Time, x.Speed)

Chart.FastLine(speed)

// The plain speed data is kinda noisy. Try a simple moving average

let movingAvgPositions = 
    positions
    |> Array.windowed 30
    |> Array.map (fun x -> 
        let last = Array.last x
        let avg = x |> Array.averageBy (fun pos -> float pos.Speed)
        { last with Speed = avg })

let movingAvg =
    movingAvgPositions 
    |> Array.map (fun x -> x.Time, x.Speed)

Chart.FastLine(movingAvg)

///////////////////////////
/// Find distance of the actual hike


let mid = (Seq.length speed) / 2

let startPosition =
    movingAvgPositions
    |> Seq.take mid
    |> Seq.findBack (fun pos -> pos.Speed > 10.)

let endPosition =
    movingAvgPositions
    |> Seq.skip mid
    |> Seq.find (fun pos -> pos.Speed > 10.)



/////////////////////////
/// Create a KML file with a line showing the entire hike

let makePlacemark name position =
    let point = new Point()
    let lat, long = position.LatLong
    let vector = new Vector(lat, long, position.Height)
    point.Coordinate <- vector
    let placemark = new Placemark()
    placemark.Geometry <- point
    placemark.Name <- name
    placemark

let rowsToKml rows =
    let features = new Folder()
    
    let placeMark = new Placemark()
    let line = new LineString()
    line.Coordinates <- new CoordinateCollection()
    for row in rows do
        let lat, long = row.LatLong
        let vector = new Vector(lat, long, row.Height)
        line.Coordinates.Add(vector)
    placeMark.Geometry <- line
    features.AddFeature(placeMark)

    let startPlacemark = makePlacemark "Start Hike" startPosition
    let endPlacemark = makePlacemark "End Hike" endPosition

    features.AddFeature(startPlacemark)
    features.AddFeature(endPlacemark)

    let kml = new Kml()
    kml.Feature <- features
    kml


let writeKml (filename: string) (kml: SharpKml.Dom.Kml) = 
    let serializer = new Serializer()
    serializer.Serialize(kml)
    System.IO.File.WriteAllText(filename, serializer.Xml)

let path = __SOURCE_DIRECTORY__ + "\\hike.kml"

positions
|> rowsToKml
|> writeKml path

System.Diagnostics.Process.Start(path)


///////////////////////
/// Write data to a database using the SqlClient TypeProvider

#r "packages/FSharp.Data.SqlClient.1.8.1/lib/net40/FSharp.Data.SqlClient.dll"
#r "packages/Microsoft.SqlServer.Types.10.50.1600.1/lib/net20/Microsoft.SqlServer.Types.dll"

open FSharp.Data
open FSharp.Data.SqlClient
open Microsoft.SqlServer.Types

[<Literal>]
let connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Tracks;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"

type InsertTrackCommand = SqlCommandProvider< @"
    insert into Track (Name)
    output Inserted.TrackId
    values (@name)
    ", connectionString>

type InsertTrackPositionCommand = SqlCommandProvider< @"
    insert into TrackPosition (TrackId, DateTime, LatLong, Height, Speed, Heading)
    output Inserted.TrackPositionId
    values (@trackId, @dateTime, @latLong, @height, @speed, @heading)
    ", connectionString>

let insertTrack name = 
    use cmd = new InsertTrackCommand()
    cmd.Execute(name) |> Seq.head

let latLongToSqlGeography (lat, long) = SqlGeography.Point(lat, long, 4326)

let insertTrackPosition trackId position =
    use cmd = new InsertTrackPositionCommand()
    let sqlGeoPoint = latLongToSqlGeography position.LatLong
    cmd.Execute(trackId, position.Time, sqlGeoPoint, position.Height, position.Speed, position.Heading) |> Seq.head

let trackId = insertTrack "Baru Hike" 

positions
|> Array.iter (fun position -> 
    insertTrackPosition trackId position |> ignore)
