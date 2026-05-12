import json

# 测试从数据库中提取的JSON字符串
test_json = '{"StartAddress":"常州市武进区前黄镇""Waypoints":["茅山风景区""江苏科技大学长山校区""北固山（江苏省镇江市京口区东吴路3号）"]"RouteMode":"driving""ReturnToStart":true}'

try:
    parsed = json.loads(test_json)
    print("JSON is valid!")
    print(f"Parsed: {parsed}")
except json.JSONDecodeError as e:
    print(f"JSON is invalid: {e}")
    print(f"Error position: {e.pos}")
    print(f"Error line: {e.lineno}")
    print(f"Error column: {e.colno}")