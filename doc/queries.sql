SELECT COUNT(DISTINCT(l.src))
FROM (
		SELECT l1.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l2.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l3.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l4.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
			JOIN Link l4 ON l4.dst = l3.src
		WHERE p.title='Jesus'
	UNION ALL
		SELECT l5.src
		FROM Page p
			JOIN Link l1 ON p.id = l1.dst
			JOIN Link l2 ON l2.dst = l1.src
			JOIN Link l3 ON l3.dst = l2.src
			JOIN Link l4 ON l4.dst = l3.src
			JOIN Link l5 ON l5.dst = l4.src
		WHERE p.title='Jesus'
) l;

SELECT *
FROM Page p
	JOIN (
		SELECT DISTINCT(l.src)
		FROM (
				SELECT l1.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l2.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l3.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l4.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
					JOIN Link l4 ON l4.dst = l3.src
				WHERE p.title='Jesus'
			UNION ALL
				SELECT l5.src
				FROM Page p
					JOIN Link l1 ON p.id = l1.dst
					JOIN Link l2 ON l2.dst = l1.src
					JOIN Link l3 ON l3.dst = l2.src
					JOIN Link l4 ON l4.dst = l3.src
					JOIN Link l5 ON l5.dst = l4.src
				WHERE p.title='Jesus'
		) l
	) l ON p.id = l.src;

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
