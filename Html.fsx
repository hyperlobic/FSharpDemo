#r "../packages/FSharp.Data.2.2.5/lib/net40/FSharp.Data.dll"

open FSharp.Data

[<Literal>]
let BloombergEnergyUrl = "http://www.bloomberg.com/energy"

type BloombergEnergy = HtmlProvider<BloombergEnergyUrl>
let energy = BloombergEnergy.Load(BloombergEnergyUrl)

energy.Tables.``Crude Oil & Natural Gas``.Rows
let first = energy.Tables.``Crude Oil & Natural Gas``.Rows |> Seq.head
first.Index
first.Price
first.``Time (EDT)``

[<Literal>]
let BloombergGoldUrl = "http://www.bloomberg.com/markets/commodities/futures/metals"

