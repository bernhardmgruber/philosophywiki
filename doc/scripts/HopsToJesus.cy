MATCH (p:Page {title:'Jesus'})
MATCH (p)<-[:links_to*1..4]-(a:Page)
RETURN DISTINCT a;