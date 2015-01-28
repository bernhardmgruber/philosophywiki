SELECT DISTINCT *
FROM (
SELECT a.*
FROM Page p
  JOIN Link l ON p.id=l.dst
  JOIN Page a ON a.id=l.src
WHERE p.title='Philosophie'
 UNION ALL
SELECT a.*
FROM Page p
  JOIN Link l ON p.id=l.dst
  JOIN Link h ON h.dst=l.src
  JOIN Page a ON a.id=h.src
WHERE p.title='Philosophie'
 UNION ALL
SELECT a.*
FROM Page p
  JOIN Link l ON p.id=l.dst
  JOIN Link h1 ON h1.dst=l.src
  JOIN Link h2 ON h2.dst=h1.src
  JOIN Page a ON a.id=h2.src
WHERE p.title='Philosophie'
 UNION ALL
SELECT a.*
FROM Page p
  JOIN Link l ON p.id=l.dst
  JOIN Link h1 ON h1.dst=l.src
  JOIN Link h2 ON h2.dst=h1.src
  JOIN Link h3 ON h3.dst=h2.src
  JOIN Page a ON a.id=h3.src
WHERE p.title='Philosophie'
) a;




WITH temp (id) AS (
  SELECT p.id
  FROM Page p
  WHERE p.title='Philosophie'
 UNION ALL
  SELECT fl.src
  FROM FirstLink fl
    JOIN temp t ON fl.dst=t.id
)
SELECT COUNT(DISTINCT(a.id))
FROM Page a
  JOIN temp t ON a.id=t.id;
