WITH temp (id) AS (
		SELECT p.id
		FROM Page p
		WHERE p.title='Philosophie'
	UNION ALL
		SELECT fl.src
		FROM FirstLink fl
			JOIN temp t ON fl.dst=t.id
)
SELECT COUNT(DISTINCT(t.id)) - 1
FROM temp t;