SELECT level
  FROM roles
  WHERE id in ($ids)
  ORDER BY level DESC
  LIMIT 1;