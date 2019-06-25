# Materialized Views with Cosmos DB Change Feeds
An example showing how a materialized view can be kept updated in near-real time in a serverless environment using:
* Azure Function
* Cosmos DB

SampleApp.DeviceSimulator inserts two types of data into ComsosDB.  
SampleApp.Functions processes the change feed received from CosmosDB to update a different view stored in CosmosDB.
