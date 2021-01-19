SELECT id, chokes
  FROM users
  WHERE id in ($ids)
  ORDER BY chokes DESC
  LIMIT $num;