# UnifiedDataHub
UnifiedDataHub is a service-oriented middleware designed to establish a standardized approach for accessing, writing, and notifying data across various application domains. By adopting Web Services and a Web-based resource structure built upon open standards, UnifiedDataHub promotes interoperability and open data principles. This ensures that data access and application development consistently follow the same approach, enabling seamless data accessibility and facilitating the creation of new services and applications across diverse domains.
![image](https://github.com/guilhermegui08/UnifiedDataHub/assets/112128696/c2f0ec71-0f99-4baa-b9b1-2c9c0fbb4bf3)
![Diagrama sem nome drawio](https://github.com/guilhermegui08/UnifiedDataHub/assets/112128696/39941157-2dc7-42f6-95e8-aaeb6158478d)

## Project Aim:
The project aims to create a service-oriented middleware that standardizes data access, writing, and notification across various application domains to promote interoperability and open data principles.

## Resource Structure:
The middleware supports a hierarchical structure consisting of applications, containers, data records, and subscriptions. Applications can have multiple containers, and containers can contain data records and subscription mechanisms.
![image](https://github.com/guilhermegui08/UnifiedDataHub/assets/112128696/c954cb91-8441-479c-b2cd-42876f980300)

## Resource Properties:
Each resource type (application, container, data, subscription) has specific properties such as ID, name, creation date, and parent relationship. Data and subscription resources do not allow update operations.

## RESTful API:
The middleware provides a RESTful API for creating, modifying, listing, deleting, and discovering each available resource. The API endpoints follow a structured URL format, with different operations distinguished by the presence of virtual references in the URL.

## Subscription Mechanism:
Subscriptions support two types of events (creation or deletion) and can fire notifications via MQTT or HTTP endpoints. Notifications include the data resource and the type of event (creation or deletion), and the channel name matches the path to the source container resource.
 Persistence: The middleware persists resources and their data in a database.

## Data Format:
Transferred data adopts the XML format.

## HTTP Actions:
HTTP actions in RESTful requests identify the target resource using the resource's unique name instead of its ID.

## Discovery Operation:
The GET HTTP verb, along with the HTTP header "somiod-discover: <res_type>", enables resource discovery. The response is a list of resource names based on the specified resource type.

## Resource Properties:
Each resource has specific properties, including a parent property storing the unique ID of the parent resource, unique ID and name properties, simplified ISO date format for datetime, numeric values for the event property (1 for creation, 2 for deletion), MQTT and HTTP endpoint properties, automatic generation of unique names for resources without one, and res_type property indicating the resource type in HTTP body.
