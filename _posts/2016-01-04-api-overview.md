---
title: "api overview"
bg: green 
color: black
fa-icon: codepen
---

# **it's got an easy to use REST API**

- To insert new JSON content, use the `POST /db/{collection}` endpoint. ExpandoDB will auto-create the target Content Collection if it doesn't exist.  
  ![Post Spec](img/post-spec.png)
- To find out what fields comprise the schema of a Content Collection are, use the `GET /db/_schemas/{collection}` endpoint.  
  ![Get Schema](img/get-schema.PNG)
- To find out what Content Collection are in the Database and what their schemas are, use the `GET /db/_schemas` endpoint.
  ![Get Schemas](img/get-schemas.png)    
- To search a Content Collection, use the `GET /db/{collection}` endpoint.  
  ![Search Collection](img/search-collection.png)
- To count items in a Content Collection, use the `GET /db/{collection}/count` endpoint.  
  ![Get Collection Count](img/get-collection-count.png)
- To retrieve a specific Content from a Content Collection, use the `GET /db/{collection}/{id}` endpoint.  
  ![Get Content](img/get-content.png)    
- To update existing JSON content, use the `PUT /db/{collection}/{id}` and `PATCH /db/{collection}/{id}` endpoints.
  ![Update Content](img/update-content.png)
- To remove existing JSON content or to remove a content collection, use the `DELETE /db/{collection}/{id}` and `DELETE /db/{collection}` endpoints.
  ![Remove Content](img/remove-content.png) 
- Check out the full [Swagger API spec](http://localhost:9000/api-spec/index.html) to learn more.
