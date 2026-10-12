adjacent_list = {
    'A':['B'],
    'B':['A', 'C', 'D'],
    'C':['B', 'D', 'E'],
    'D':['B', 'C', 'F'],
    'E':['C', 'F', 'G', 'H'],
    'F':['D', 'E', 'G'],
    'G':['E', 'F', 'I'],
    'H':['E', 'I', 'J'],
    'I':['G', 'H', 'J'],
    'J':['H', 'I', 'K'],
    'K':['J']
    }

# value_list = {
#     'A':0,
#     'B':1,
#     'C':1,
#     'D':2,
#     'E':2,
#     'F':3,
#     'G':4,
#     'H':7,
#     'I':7,
#     'J':6,
#     'K':7
#     }

value_list = {
    'A':2,
    'B':1,
    'C':1,
    'D':2,
    'E':0,
    'F':3,
    'G':4,
    'H':7,
    'I':5,
    'J':6,
    'K':7
    }


def depth_first_search(target_node, direction, visited=[]):
    #search possible chain from one direction(backward or forward) using depth first search allogaritm
    visited.append(target_node)
    long_paths = [[]]

    neighbors = adjacent_list[target_node]
    for neighbor in neighbors:
        if neighbor not in visited:
            expected_phase = (value_list[target_node]+direction+8) % 8
            if value_list[neighbor] == expected_phase:
                sub_path = depth_first_search(neighbor, direction, visited)
                if long_paths == [[]]:
                    long_paths = sub_path
                else:
                    for path in sub_path:
                        long_paths.append(path)

    visited.remove(target_node)
    for path in long_paths:
        path.insert(0, target_node)
    return long_paths

def findLunarBranch(target_node, is_start_from_backward):
    #find possible path branch of lunar phase sequence, seeking from backward first or forward first
    direction = 1
    if is_start_from_backward:
        direction = -1

    full_branch = []
    first_branch = depth_first_search(target_node, direction)
    for first_seq in first_branch:
        first_seq.remove(target_node)
        first_seq.reverse()
        second_branch = depth_first_search(target_node, -direction, first_seq)
        for second_seq in second_branch:
            full_branch.append(first_seq + second_seq)

    return full_branch

def findLunarChains(target_node):
    #find possible lunar sequence chain from target node
    global adjacent_list, value_list

    backward_branch = findLunarBranch(target_node, True)
    forward_branch = findLunarBranch(target_node, False)

    #filter only unique sequence of lunar phase
    full_branch = backward_branch + forward_branch
    filter_seq = set()
    unique_seq = []
    for sequence in full_branch:
        hashable_seq = frozenset(sequence)
        if hashable_seq not in filter_seq and len(sequence) >= 3:
            unique_seq.append(sequence)
            filter_seq.add(hashable_seq)

    print("Unique Lunar Sequence: ")
    for sequence in unique_seq:
        print(sequence)

    return unique_seq
# import time

# start_time = time.time()
# findLunarChains('F')
# end_time = time.time()
# print(f"Execution time: {end_time - start_time:.4f} seconds")
import time

start_cpu = time.process_time()
findLunarChains('F')
end_cpu = time.process_time()
print(f"Actual CPU overhead: {end_cpu - start_cpu:.4f} seconds")
# Output:
# Unique chain:
# ['B', 'D', 'F', 'G', 'I', 'J', 'H', 'E', 'C']
# ['B', 'D', 'F', 'G', 'I', 'J', 'K']
# ['G', 'I', 'J', 'H', 'E', 'C', 'D', 'F']
# ['H', 'E', 'C', 'D', 'F', 'G', 'I', 'J', 'K']