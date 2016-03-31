---
title: "api overview"
bg: green 
color: black
fa-icon: codepen
---

# **it's got an easy to use REST API**

- To find out what's in the database, use the `GET /db/_schemas` endpoints.  
  ![Get Metadata](img/get-metadata.png)  
- To search a collection, use the `GET /db/{collection}` endpoints.  
  ![Search Content](img/search-content.png)
- To insert new JSON content, use the `POST /db/{collection}` endpoint. ExpandoDB auto-creates the target Content Collection if it doesn't exist.  
  ![Post Spec](img/post-spec.png)
- To update existing JSON content, use the `PUT /db/{collection}/{id}` and `PATCH /db/{collection}/{id}` endpoints.
  ![Update Content](img/update-content.png)
- To remove existing JSON content or to remove a content collection, use the `DELETE /db/{collection}/{id}` and `DELETE /db/{collection}` endpoints.
  ![Remove Content](img/remove-content.png) 
- Check out the full [Swagger API spec](http://localhost:9000/api-spec/index.html) to learn more.