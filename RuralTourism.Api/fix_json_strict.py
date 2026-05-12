import sqlite3
import json

# 连接到数据库
conn = sqlite3.connect('RuralTourism.db')
cursor = conn.cursor()

# 获取所有有问题的TourPlan记录
cursor.execute("SELECT Id, WaypointsJson FROM TourPlans WHERE WaypointsJson IS NOT NULL")
rows = cursor.fetchall()

print(f"Found {len(rows)} tour plans to check")

for row in rows:
    tour_plan_id, waypoints_json = row
    
    if not waypoints_json:
        continue
    
    try:
        # 尝试严格解析JSON
        parsed = json.loads(waypoints_json)
        print(f"ID {tour_plan_id}: JSON is valid")
    except json.JSONDecodeError as e:
        print(f"ID {tour_plan_id}: Invalid JSON - {e}")
        print(f"Original JSON: {waypoints_json[:100]}...")
        
        # 尝试修复JSON格式
        try:
            # 替换缺失的逗号
            fixed_json = waypoints_json
            
            # 修复各种缺失逗号的情况
            fixed_json = fixed_json.replace('""""', '","')
            fixed_json = fixed_json.replace('""Waypoints', ',"Waypoints')
            fixed_json = fixed_json.replace('""RouteMode', ',"RouteMode')
            fixed_json = fixed_json.replace('""ReturnToStart', ',"ReturnToStart')
            
            # 验证修复后的JSON
            parsed = json.loads(fixed_json)
            print(f"ID {tour_plan_id}: Successfully fixed JSON")
            print(f"Fixed JSON: {fixed_json[:100]}...")
            
            # 更新数据库
            cursor.execute("UPDATE TourPlans SET WaypointsJson = ? WHERE Id = ?", (fixed_json, tour_plan_id))
            conn.commit()
        except Exception as fix_error:
            print(f"ID {tour_plan_id}: Failed to fix JSON - {fix_error}")
            # 如果修复失败，设置为空数组
            cursor.execute("UPDATE TourPlans SET WaypointsJson = '[]' WHERE Id = ?", (tour_plan_id,))
            conn.commit()

conn.close()
print("Database fix completed")