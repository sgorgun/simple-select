SELECT
	name,
	surname
FROM
	person
WHERE
	surname LIKE 'Kra%'
ORDER BY
	birth_date DESC;