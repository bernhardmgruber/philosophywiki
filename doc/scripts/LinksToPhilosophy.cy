MATCH (p:Page {title:'Philosophie'})
MATCH (p)<-[:first_links_to*]-(a:Page)
RETURN DISTINCT a;