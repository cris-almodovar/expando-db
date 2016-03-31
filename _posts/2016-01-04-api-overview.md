---
title: "api overview"
bg: green 
color: black
fa-icon: codepen
---

# **it's got an easy to use REST API**

- To insert new JSON content, use the `POST /db/{collection}` endpoint. ExpandoDB will auto-create the target Content Collection if it doesn't exist.  
  ![Post Spec](img/post-spec.png)
- To find out what fields comprise the schema of a specific Content Collection, use the `GET /db/_schemas/{collection}` endpoint.  
  ![Get Schema](img/get-schema.png)
- To find out what Content Collections are in the Database and what their schemas are, use the `GET /db/_schemas` endpoint.
  ![Get Schemas](img/get-schemas.png)    
- To search a Content Collection, use the `GET /db/{collection}` endpoint.  
  ![Search Collection](img/search-collection.png)
- To count items in a Content Collection, use the `GET /db/{collection}/count` endpoint.  
  ![Get Collection Count](img/get-collection-count.png)
- To retrieve a specific Content from a Content Collection, use the `GET /db/{collection}/{id}` endpoint.  
  ![Get Content](img/get-content.png)    
- To update an existing JSON content, use the `PUT /db/{collection}/{id}` endpoint. The JSON content that you pass in will completely
  replace the existing one.
  ![Put Content](img/put-collection.png)
- To partially update an existing JSON content, use the `PATCH /db/{collection}/{id}` endpoint. The JSON content that you pass in will be merged
  with the existing one.
  ![Patch Content](img/patch-collection.png)
- To remove an existing JSON content, use the `DELETE /db/{collection}/{id}` endpoint.
  ![Delete Content](img/delete-collection.png) 
- To remove an entire Content Collection, use the `DELETE /db/{collection}` endpoint.
  ![Drop Collection](img/drop-collection.png) 
- Check out the [Swagger API spec](http://localhost:9000/api-spec/index.html) to try out the endpoints.
